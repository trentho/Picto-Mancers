from typing import Optional
import numpy as np
import numpy.typing as npt

class GestureDrawing:
    """
    Class for storing, modifying, and transforming a drawing
    """
    def __init__(self) -> None: ...
    
    def add_point(self, x: float, y: float) -> None:
        """
        Adds a point to the drawing. Assumes +x is right and +y is down
        """
    
    def last(self) -> Optional[tuple[float, float]]:
        """
        Returns the last point in the drawing
        """
    
    def __len__(self) -> int:
        """
        Returns the number of points in the drawing
        """
    
    def __str__(self) -> str:
        """
        Returns a list of all the points in the drawing in a string
        """
    
    def __repr__(self) -> str:
        """
        Same as serialized()
        """

    def copy(self) -> GestureDrawing:
        """
        Makes a copy of the GestureDrawing
        """

    def clear(self) -> GestureDrawing:
        """
        Clears all points
        """
    
    @property
    def aabb(self) -> tuple[float, float, float, float]:
        """
        The Axis Aligned Bounding Box of the drawing: min_x, min_y, max_x, max_y
        """
    
    def normalized(self, size: float) -> GestureDrawing:
        """
        Scales the drawing so that all its points fit inside 
        a box of size "size" with the min corner at the origin
        """
    
    def translated(self, x: float, y: float) -> GestureDrawing:
        """
        Translates the drawing
        """

    def reflected(self) -> GestureDrawing:
        """
        Reflects the drawing horizontally across its center
        """
    
    def rotated(self, theta: float) -> GestureDrawing:
        """
        Rotates the drawing about the center
        """

    def simplified(self, epsilon: float) -> GestureDrawing:
        """
        Simplifies the drawing using the Ramer-Douglas-Peucker algorithm
        """

    def serialized(self) -> npt.NDArray[np.float64]:
        """
        Serializes into numpy array of size (x, 2)
        """

    @staticmethod
    def deserialize(data: npt.NDArray[np.float64]) -> GestureDrawing:
        """
        Deserializes a numpy array of size (x, 2)
        """

    def rasterized(self) -> npt.NDArray[np.float64]:
        """
        Draws value=1.0 onto a value=0.0 28x28 square.
        Make sure to normalize to size 28 before calling
        so that the entire drawing is visible
        """