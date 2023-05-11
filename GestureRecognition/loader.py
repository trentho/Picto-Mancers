# Script with utils for loading and saving gesture drawings and models

import os
import numpy as np
import numpy.typing as npt
import tensorflow as tf
import tf2onnx
import onnxruntime as ort
from gesture_drawing import GestureDrawing

def gesture_path(name: str): 
    return os.path.join("gestures", name+".npz")

def load_gesture(name: str) -> list[GestureDrawing]:
    """
    Loads all drawings in gestures/{name}.npz
    """
    if not os.path.exists(gesture_path(name)):
        return []
    with np.load(gesture_path(name)) as data:
        keys = sorted(data.files, key=lambda x: int(x[4:]))
        return [GestureDrawing.deserialize(data[key]) for key in keys]
    
def save_gesture(name: str, drawings: list[GestureDrawing]):
    """
    Saves the list of drawings to gestures/{name}.npz
    """
    if not os.path.exists('gestures'):
        os.mkdir('gestures')
    data = [drawing.serialized() for drawing in drawings]
    np.savez(gesture_path(name), *data)

def model_path_base(name: str): 
    return os.path.join("models", name)

def load_classes(name: str) -> list[str]:
    """
    Loads the class names from models/{name}.txt
    """
    with open(model_path_base(name) + ".txt", 'r') as infile:
        return [line.rstrip('\n') for line in infile]
    
def load_model(name: str) -> tf.keras.Model:
    """
    Loads a keras model from models/{name}.h5 
    with class names from models/{name}.txt
    """
    model: tf.keras.Model = tf.keras.models.load_model(model_path_base(name) + ".h5")
    model._name = name
    return model

def load_onnx(name: str) -> ort.InferenceSession:
    """
    Creates an ONNX inference session from models/{name}.txt
    """
    return ort.InferenceSession(model_path_base(name) + ".onnx")

def save_model(model: tf.keras.Model, class_names: list[str]):
    """
    Saves a model and list class names into an .h5, .onnx, and .txt file in models/.
    Uses the model's name for the file name
    """
    base = model_path_base(model.name)
    print("Saving model to", base)
    with open(base + ".txt", 'w') as outfile:
        outfile.writelines([class_name+'\n' for class_name in class_names])
    model.save(base + ".h5")
    input_signature = (tf.TensorSpec((None, 28, 28, 1), tf.float32, name="input"),)
    tf2onnx.convert.from_keras(model, input_signature=input_signature, output_path=base + ".onnx")
    
