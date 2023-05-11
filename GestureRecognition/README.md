# Gesture Recognizer

CNN first trained with Google's QuickDraw dataset, then transferred to a model trained on a specific dataset of drawings.

## drawing_app.py

The utility app for creating training data and testing a trained model. Run it by running `python drawing_app.py`.

- To create training data, select or add a spell on the left sidebar. Toggle the button on the bottom left so that it says "View Drawings". Then draw on the canvas in the middle and press the space bar or enter key to save it to the selected spell.
- To view training data, select a spell on the left side bar and toggle the button on the bottom left so that it says "Back to Canvas". Right click on drawings in this view to delete drawings

## model.ipynb

Where the model is trained. Run through it and the model should be saved to `weights/spell_model`. Here are some pretrained models that can be used (using doodleNet for the game):

quickdraw14: https://github.com/akshaybahadur21/QuickDraw

quickdraw18: https://github.com/uvipen/QuickDraw-AirGesture-tensorflow

quickdraw20: https://github.com/uvipen/QuickDraw

quickdraw_doodlenet: https://github.com/yining1023/doodleNet

quickdraw_sketchanet: https://github.com/dasayan05/sketchanet-quickdraw
- no pre-trained weights

## gesture_drawing/

A python module written in Rust that creates and rasterizes drawings based on a custom algorithm. This algorithm is the same as the one used in the game to provide consistency.

In this directory, `maturin develop` to quickly test changes (will be slower). `maturin build` and `pip install target/wheels/gesture_drawing-{VERSION}.tar.gz` to install and import in python.