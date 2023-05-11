import tkinter as tk
import math
from gesture_drawing import GestureDrawing
from loader import load_gesture, save_gesture, load_onnx, load_classes
from typing import Generic, TypeVar, Callable, Type
from glob import glob
import os
from PIL import Image, ImageTk
from onnxruntime import InferenceSession

T = TypeVar('T')


class ChangingVar(Generic[T]):
	def __init__(self, value: T | None = None):
		self.value = value
		self.callbacks: list[Callable[[T | None, T | None], None]] = []
	def set(self, value: T | None):
		if value == self.value: return
		old_value = self.value
		self.value = value
		for callback in self.callbacks:
			callback(old_value, self.value)
	def get(self):
		return self.value
	def getreq(self) -> T:
		if self.value is None:
			raise Exception("Value is None when required to be set")
		return self.value
	def trace(self, callback: Callable[[T | None, T | None], None]):
		self.callbacks.append(callback)

window = tk.Tk()
window.title('Gesture Maker')
screen_width, screen_height = window.winfo_screenwidth(), window.winfo_screenheight()
window.geometry(f'904x560+{int(screen_width/2)-452}+{int(screen_height/2)-280}')
window.columnconfigure(1, weight=1)
window.rowconfigure(0, weight=1)
tk.Label(window, text="Select or create a gesture to the left to begin").grid(row=0, column=1, sticky='nsew')

use_cairo = ChangingVar(False)
window.bind("c", lambda _: use_cairo.set(not use_cairo.getreq()))

viewing_mode = ChangingVar(False)
show_main_widget = ChangingVar(False)
main_widget: tk.Widget | None = None
def update_main_widget(*_):
	def set_widget(Widget: Type[tk.Widget]):
		global main_widget
		main_widget = Widget(window)
		main_widget.grid(row=0, column=1, pady=3, sticky='nsew')

	view, show = viewing_mode.getreq(), show_main_widget.getreq()
	if not show:
		if main_widget != None: main_widget.destroy()
	else:
		set_widget(GestureViewer if view else GestureDrawingCanvas)
viewing_mode.trace(update_main_widget)
show_main_widget.trace(update_main_widget)


gestures: list[str] = [] # gesture names
for file in glob(os.path.join("gestures", "*.npz")):
	gestures.append(os.path.splitext(os.path.basename(file))[0])


selected_gesture = ChangingVar[int]()

# GestureDrawings for the currently selected gesture
selected_gesture_drawings: list[GestureDrawing] = []
def _update_selected_gesture_drawings(_, gesture: int | None):
	global selected_gesture_drawings
	if gesture != None:
		selected_gesture_drawings = load_gesture(gestures[gesture])
	# show main widget if valid gesture is selected
	show_main_widget.set(gesture != None)
selected_gesture.trace(_update_selected_gesture_drawings)

# Drawing to be displayed in the right side bar
selected_drawing = ChangingVar[GestureDrawing]()


recognizer_classes: list[str] = None
recognizer: InferenceSession = None


class GestureDrawingImage(tk.Canvas):
	"""
	Static image of a GestureDrawing
	"""
	def __init__(self, master: tk.Misc, drawing: GestureDrawing):
		tk.Canvas.__init__(self, master, bg='white')
		self._width_cache, self._height_cache = 0, 0
		self.drawing = drawing
		self.bind('<Configure>', lambda event: self.resize(event.width, event.height))
		use_cairo.trace(lambda _, __: self._redraw())
	
	def _redraw(self):
		self.delete('all')
		s = min(self.winfo_width(), self.winfo_height())
		raster = self.drawing.normalized(23).translated(2.5,2.5).rasterized() * 255
		image = Image.fromarray(raster).resize((s,s), resample=Image.Resampling.NEAREST)
		self.image = ImageTk.PhotoImage(image=image)
		self.create_image(0,0, image=self.image, anchor=tk.NW)
	
	def set_drawing(self, drawing: GestureDrawing):
		self.drawing = drawing
		self._redraw()
	
	def resize(self, width, height): # redraw if height/width changed
		if width == self._width_cache and height == self._height_cache: return
		self._width_cache, self._height_cache = width, height
		self._redraw()


class GestureDrawingCanvas(tk.Canvas):
	"""
	Canvas where the user can draw a GestureDrawing
	"""
	def __init__(self, master: tk.Misc):
		super().__init__(master, bg='white')
		self._line_id = None
		self._prediction_text_id = None
		self.drawing = GestureDrawing()
		self.is_drawing = False
		self.bind('<Button-1>', self.on_button_down)
		self.bind('<ButtonRelease-1>', self.on_button_up)
		self.bind('<B1-Motion>', self.record_motion)
		self.bind('<Return>', self.save_drawing)
		self.bind('<space>', self.save_drawing)
		# self.bind("c", lambda _: use_cairo.set(not use_cairo.getreq()))

	def _redraw(self):
		self.delete('all')
		if len(self.drawing) < 2: return
		self.create_line(*self.drawing.serialized().flatten(), 
		   fill='black', width=5, capstyle=tk.ROUND, joinstyle=tk.ROUND)
		# selected_drawing.set(self.drawing.copy())

	def _clear(self):
		self.drawing.clear()
		self._redraw()

	def _add_point(self, x, y):
		self.drawing.add_point(x, y)
		self._redraw()

	def on_button_down(self, event):
		self.focus_set()
		self._clear()
		self._add_point(event.x, event.y)
		self.is_drawing = True

	def on_button_up(self, _):

		x1, y1, x2, y2 = self.drawing.aabb
		s = max(x2-x1, y2-y1)
		self.drawing = self.drawing.simplified(0.01*s)
		selected_drawing.set(self.drawing.copy())
		self._redraw()

		global recognizer_classes, recognizer
		if recognizer_classes is None: recognizer_classes = load_classes("gesture_recognizer")
		if recognizer is None: recognizer = load_onnx("gesture_recognizer")

		data = self.drawing.normalized(23).translated(2.5,2.5).rasterized()
		data = data.reshape(1, 28, 28, 1).astype('float32')
		predictions = recognizer.run(None, {'input': data})

		text = "Prediction:"
		for i, p in enumerate(predictions[0][0]):
			text += f"\n{recognizer_classes[i]}: {p*100:0.2f}"
		self.create_text(5, 5, anchor='nw', text=text, fill='black')

		self.is_drawing = False

	def record_motion(self, event):
		if not self.is_drawing:
			return
		x, y, p = event.x, event.y, self.drawing.last()
		if (p == None or abs(x - p[0]) + abs(y - p[1]) > 3):
			self._add_point(x, y)

	def save_drawing(self, _):
		if self.is_drawing:
			return
		if len(self.drawing) > 1:
			selected_gesture_drawings.append(self.drawing.normalized(1))
			save_gesture(gestures[selected_gesture.getreq()], selected_gesture_drawings)
		self._clear()


class GestureViewer(tk.Frame):
	def __init__(self, master: tk.Misc):
		super().__init__(master)
		self.grid_rowconfigure(0, weight=1)
		self.grid_columnconfigure(0, weight=1)
		self.grid_propagate(False)

		self.scroll_area = tk.Canvas(self, takefocus=False)
		self.scroll_area.grid(row=0, column=0, sticky="news")

		scrollbar = tk.Scrollbar(self, orient=tk.VERTICAL, command=self.scroll_area.yview)
		scrollbar.grid(row=0, column=1, sticky='ns')
		self.scroll_area.config(yscrollcommand=scrollbar.set)

		self.frame = tk.Frame(self.scroll_area)
		self.frame.grid_propagate(False)
		self.scroll_area.create_window((0, 0), window=self.frame, anchor='nw')

		self.scroll_area.bind('<Configure>', lambda _: self.update_grid_size())
		
		self.redraw_grid()

	@property
	def col_count(self): return min(5, max(1, len(selected_gesture_drawings)))

	@property
	def row_count(self): return math.ceil(len(selected_gesture_drawings) / self.col_count)

	def update_grid_size(self):
		width = self.scroll_area.winfo_width()
		height = int(width * self.row_count / self.col_count)
		self.frame.config(width=width-10, height=height-10)
		self.scroll_area.config(width=width, height=height)
		self.scroll_area.config(scrollregion=self.scroll_area.bbox('all'))

	def delete_drawing(self, i):
		selected_gesture_drawings.pop(i)
		save_gesture(gestures[selected_gesture.getreq()], selected_gesture_drawings)
		self.redraw_grid()

	def select_drawing(self, cell: GestureDrawingImage):
		cell.focus_set()
		selected_drawing.set(cell.drawing.copy())

	def redraw_grid(self):
		for cell in self.frame.winfo_children(): cell.destroy()

		for c in range(5): self.frame.columnconfigure(c, weight=1)
		for r in range(self.row_count): self.frame.rowconfigure(r, weight=1)

		for i, drawing in enumerate(selected_gesture_drawings):
			cell = GestureDrawingImage(self.frame, drawing)
			cell.grid(row=int(i/5), column=i%5, sticky='nsew')
			cell.bind('<Button-1>', lambda event: self.select_drawing(event.widget))
			cell.bind('<Button-2>', lambda _, i=i: self.delete_drawing(i))
		
		self.update_grid_size()




# LEFT SIDEBAR #####################################################

left_sidebar = tk.Frame(window, width=120, pady=5, padx=3)
left_sidebar.grid(row=0, column=0, sticky='nsew')
left_sidebar.grid_propagate(False)
left_sidebar.rowconfigure(1, weight=1)
left_sidebar.columnconfigure(0, weight=1)
gesture_list_names = tk.StringVar(value=gestures)


def set_gesture(idx):
	if not type(idx) is int:
		idx = None if len(idx) == 0 else idx[0]
	if viewing_mode.getreq(): 
		selected_gesture.set(None) # set to none to recreate viewer
	selected_gesture.set(idx)

def add_gesture():
	popup = tk.Toplevel(window, padx=10, pady=10)
	popup.resizable(False, False)
	popup.title('Add Gesture')
	popup.grab_set()
	
	x = int(window.winfo_x() + window.winfo_width()/2 - 120)
	y = int(window.winfo_y() + window.winfo_height()/2 - 50)
	popup.geometry(f'240x100+{x}+{y}')

	popup.columnconfigure(0, weight=1)
	popup.columnconfigure(1, weight=1)

	gesture_entry_name = tk.StringVar()
	label_text = tk.StringVar(value="Enter the name of the gesture to create")
	def submit():
		gesture = gesture_entry_name.get().strip()
		if gesture in gestures or gesture == "":
			label_text.set("Invalid or exists. please try again")
		else:
			gestures.append(gesture)
			gesture_list_names.set(gestures)
			set_gesture(len(gestures)-1)
			popup.destroy()

	tk.Label(popup, textvariable=label_text).grid(row=0, column=0, columnspan=2)
	entry = tk.Entry(popup, textvariable=gesture_entry_name)
	entry.grid(row=1, column=0, columnspan=2, sticky='ew')
	entry.bind('<Return>', lambda _: submit())
	entry.focus_set()
	tk.Button(popup, text="Cancel", width=1, command=lambda: popup.destroy()).grid(row=2, column=0, sticky='ew')
	tk.Button(popup, text="Ok", width=1, command=submit).grid(row=2, column=1, sticky='ew')

add_gesture_button = tk.Button(left_sidebar, text="Add Gesture", command=add_gesture)
add_gesture_button.grid(row=0, column=0, sticky='ew')

gesture_list = tk.Listbox(left_sidebar, listvariable=gesture_list_names)
gesture_list.grid(row=1, column=0, sticky='nsew', pady=5)
gesture_list.bind('<<ListboxSelect>>', lambda _: set_gesture(gesture_list.curselection()))
def _update_gesture_list_selection(old_gesture: int | None, new_gesture: int | None):
	if old_gesture != None: gesture_list.selection_clear(old_gesture)
	if new_gesture != None: gesture_list.selection_set(new_gesture)
selected_gesture.trace(_update_gesture_list_selection)

view_drawings_button = tk.Button(left_sidebar, text="View Drawings", height=2, 
				 command=lambda: viewing_mode.set(not viewing_mode.getreq()))
view_drawings_button.grid(row=2, column=0, sticky='ew')
viewing_mode.trace(lambda _,view: view_drawings_button.config(
	text="Back to Canvas" if view else "View Drawings"))



# RIGHT SIDEBAR #####################################################

left_sidebar = tk.Frame(window, pady=5, padx=3)
left_sidebar.grid(row=0, column=2, sticky='nsew')
left_sidebar.grid_propagate(False)
left_sidebar.rowconfigure(1, weight=1)
left_sidebar.columnconfigure(0, weight=1)
left_sidebar.bind('<Configure>', lambda event: left_sidebar.config(width=int(event.height*2/5), height=event.height))

for c in range(2): left_sidebar.columnconfigure(c, weight=1)
for r in range(5): left_sidebar.rowconfigure(r, weight=1)

preview_cells: list[list[GestureDrawingImage]] = []
for r in range(5):
	preview_cells.append([])
	for c in range(2):
		cell = GestureDrawingImage(left_sidebar, GestureDrawing())
		cell.grid(row=r, column=c, sticky='nsew')
		preview_cells[r].append(cell)

def _update_left_sidebar(_, drawing: GestureDrawing):
	reflected = drawing.reflected()
	for r in range(5):
		preview_cells[r][0].set_drawing(drawing.rotated((r-2) * math.pi/18))
		preview_cells[r][1].set_drawing(reflected.rotated((r-2) * math.pi/18))

selected_drawing.trace(_update_left_sidebar)


window.mainloop()
print("started")