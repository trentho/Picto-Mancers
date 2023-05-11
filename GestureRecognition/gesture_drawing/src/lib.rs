use nalgebra::Vector2 as NAVector2;
use numpy::ndarray::{Array2, arr2};
use numpy::{PyReadonlyArray2, PyArray2, IntoPyArray};
use pyo3::prelude::*;
use ramer_douglas_peucker::rdp;
use rayon::prelude::*;

type Vector2 = NAVector2<f64>;

trait VectorProjection {
    fn project(&self, onto: Vector2) -> Vector2;
}
impl VectorProjection for Vector2 {
    fn project(&self, onto: Vector2) -> Vector2 {
        onto * self.dot(&onto) / onto.magnitude_squared()
    }
}


#[pyclass]
struct GestureDrawing {
    drawing: Vec<Vector2>,
}

impl GestureDrawing {
    fn aabb(&self) -> (Vector2, Vector2) {
        self.drawing.iter().fold(
            (
                Vector2::new(f64::INFINITY, f64::INFINITY), 
                Vector2::new(f64::NEG_INFINITY, f64::NEG_INFINITY)
            ),
            |(min, max), &p| (
                Vector2::new(min.x.min(p.x), min.y.min(p.y)), 
                Vector2::new(max.x.max(p.x), max.y.max(p.y))
            )
        )
    }
    fn map<F>(&self, f: F) -> GestureDrawing
    where F: Fn(Vector2) -> Vector2 + std::marker::Sync {
        GestureDrawing { 
            drawing: self.drawing.par_iter()
                .map(|&point| f(point))
                .collect()
        }
    }
}

#[pymethods]
impl GestureDrawing {
    #[new]
    fn new() -> Self {
        GestureDrawing { drawing: Vec::new() }
    }

    /// Assumes +x is right and +y is down
    fn add_point(&mut self, x: f64, y: f64) {
        self.drawing.push(Vector2::new(x, y))
    }
    fn last(&self) -> Option<(f64, f64)> {
        self.drawing.last().map(|&p| (p.x, p.y))
    }
    fn __len__(&self) -> usize {
        self.drawing.len()
    }
    fn __str__(&self) -> String {
        format!("{:?}", self.drawing.par_iter()
            .map(|&p| format!("({}, {})", p.x, p.y))
            .collect::<Vec<_>>()
            .join(", ")
        )
    }
    fn __repr__<'py>(&self, py: Python<'py>) -> Result<&'py pyo3::types::PyString, PyErr> {
        self.serialized(py).repr()
    }
    fn copy(&self) -> GestureDrawing {
        GestureDrawing { drawing: self.drawing.clone() }
    }
    fn clear(&mut self) {
        self.drawing.clear()
    }

    #[getter]
    fn get_aabb(&self) -> (f64, f64, f64, f64) {
        let (min, max) = self.aabb();
        (min.x, min.y, max.x, max.y)
    }

    /// Scales the drawing so that all its points fit inside 
    /// a box of size "size" with a corner at the origin
    fn normalized(&self, size: f64) -> GestureDrawing {
        let (min, max) = self.aabb();
        let dim = max - min;
        let my_size = f64::max(dim.x, dim.y).max(0.0001);
        let min = ((min+max)/2.0).add_scalar(-my_size/2.0);
        self.map(|p| (p-min) * size/my_size)
    }

    /// Translates the drawing
    fn translated(&self, x: f64, y: f64) -> GestureDrawing {
        self.map(|p| p + Vector2::new(x, y))
    }

    /// Reflects the drawing horizontally across its center
    fn reflected(&self) -> GestureDrawing {
        let (x1, _, x2, _) = self.get_aabb();
        let xc = (x1+x2)/2.0;
        self.map(|p| Vector2::new(2.0*xc-p.x, p.y))
    }

    /// Rotates the drawing about the center
    fn rotated(&self, theta: f64) -> GestureDrawing {
        let (min, max) = self.aabb();
        let center = (min+max)/2.0;
        let (cos, sin) = (theta.cos(), theta.sin());
        self.map(|mut p| {
            p -= center;
            Vector2::new(p.x*cos-p.y*sin, p.y*cos+p.x*sin) + center
        })
    }

    /// Simplifies the drawing using the Ramer-Douglas-Peucker algorithm
    fn simplified(&self, epsilon: f64) -> GestureDrawing {
        let points = self.drawing.par_iter()
            .map(|&p| mint::Point2 { x: p.x, y: p.y })
            .collect::<Vec<_>>();
        let indices = rdp(points.as_slice(), epsilon);
        GestureDrawing { 
            drawing: indices.par_iter()
                .map(|&i| self.drawing[i])
                .collect()
        }
    }

    /// Serializes into np array of size (x, 2)
    fn serialized<'py>(&self, py: Python<'py>) -> &'py PyArray2<f64> {
        arr2(self.drawing.par_iter()
            .map(|&p| [p.x, p.y])
            .collect::<Vec<_>>()
            .as_slice()
        ).into_pyarray(py)
    }

    /// Deserializes a np array of size (x, 2)
    #[staticmethod]
    fn deserialize(data: PyReadonlyArray2<f64>) -> GestureDrawing {
        let data = data.as_array();
        assert_eq!(data.shape()[1], 2);
        GestureDrawing { 
            drawing: data.outer_iter().into_par_iter()
                .map(|a| Vector2::new(a[[0]], a[[1]]))
                .collect::<Vec<_>>()
        }
    }

    /// Draws value=1.0 onto a value=0.0 28x28 square.
    /// Make sure to normalize to size 28 before calling
    /// so that the entire drawing is visible
    fn rasterized<'py>(&self, py: Python<'py>) -> &'py PyArray2<f64> {
        let width = 2.5;
        let sample_resolution = 0.6; // how much step per sample

        // Defines alpha based on the distance from the line
        let alpha_function = |mut dist: f64| {
            dist /= width / 2.0; // Normalized distance
            // (1.0 - dist*dist).max(0.0).sqrt().min(1.0)
            
            // flat until 0.25 width, then linear to zero
            let slope = 1.0 / (1.0 - 0.25); 
            (-slope * dist + slope).clamp(0.0, 1.0)
        };

        let add_line_to_raster = |mut raster: Array2<f64>, line_raster: Array2<f64>| {
            // Draw the line raster onto the main raster
            // Use max function so all lines look like they come from one
            for x in 0..28 {
                for y in 0..28 {
                    raster[[x, y]] = raster[[x, y]].max(line_raster[[x, y]]);
                }
            }
            raster
        };

        let raster = (1..self.drawing.len()).into_par_iter()
            .fold(|| Array2::<f64>::zeros((28, 28)), |raster, i| {
                let mut line_raster = Array2::<f64>::zeros((28, 28));
                let mut visited = [[false; 28]; 28];
    
                // Draw a line from p1 to p2
                let (p1, p2) = (self.drawing[i-1], self.drawing[i]);
    
                let dir = p2 - p1;
                let length = dir.magnitude();
                let dir = dir.normalize();
                let perp = Vector2::new(-dir.y, dir.x);
    
                let mut get_pixel = |l: f64, w: f64| {
                    let pixel = p1 + dir*l + perp*w;
                    // let pixel = Vector2::new(pixel.x.round(), pixel.y.round());
                    let (x, y) = (pixel.x as i32, pixel.y as i32);
                    let pixel = Vector2::new(x as f64, y as f64);
                    if x < 0 || y < 0 || x >= 28 || y >= 28 || visited[x as usize][y as usize] {
                        None
                    } else {
                        visited[x as usize][y as usize] = true;
                        Some((pixel, x as usize, y as usize))
                    }
                };
    
                // Draw the line
                for w in 0..=(width/sample_resolution) as i32 {
                    let w = -width/2.0 + (w as f64) * sample_resolution;
    
                    // Draw line
                    for l in 0..=(length/sample_resolution) as i32 {
                        let l = (l as f64) * sample_resolution;
                        if let Some((p, x, y)) = get_pixel(l, w) {
                            let dist = (p - p1).project(perp).magnitude();
                            line_raster[[x, y]] = alpha_function(dist);
                        }
                    }
    
                    // Draw caps
                    for d in 0..=(width/2.0/sample_resolution) as i32 {
                        let d = (d as f64) * sample_resolution;
                        
                        // Cap 1
                        if let Some((p, x, y)) = get_pixel(-d, w) {
                            let dist = (p - p1).magnitude();
                            line_raster[[x, y]] = alpha_function(dist);
                        }

                        // Cap 2
                        if let Some((p, x, y)) = get_pixel(length+d, w) {
                            let dist = (p - p2).magnitude();
                            line_raster[[x, y]] = alpha_function(dist);
                        }
                    }
                }

                add_line_to_raster(raster, line_raster)
            }
        )
        .reduce(
            || Array2::<f64>::zeros((28, 28)), 
            add_line_to_raster
        );

        raster.t().to_owned().into_pyarray(py)
    }
    
}


/// A Python module implemented in Rust.
#[pymodule]
fn gesture_drawing(_py: Python, m: &PyModule) -> PyResult<()> {
    m.add_class::<GestureDrawing>()?;
    Ok(())
}