using UnityEngine;

public static class LinearAlgebraUtils
{
    // Projects a Vector2 onto another Vector2
    public static Vector2 Project(this Vector2 vector, Vector2 onto) {
        return onto * Vector2.Dot(vector, onto) / onto.sqrMagnitude;
    }

    public static T[,] Transposed<T>(this T[,] matrix) {
        var (rows, columns) = (matrix.GetLength(0), matrix.GetLength(1));
        T[,] transposed = new T[columns, rows];
        for (var c = 0; c < columns; c++)
        {
            for (var r = 0; r < rows; r++)
            {
                transposed[c, r] = matrix[r, c];
            }
        }
        return transposed;
    }


    // Returns (centroid, direction of maximum variance, direction of medium variance, direction of minimum variance)
    public static (Vector3, Vector3, Vector3, Vector3) ComputePrincipalDirections(Vector3[] points) {
        int n = points.Length;

        // Compute the centroid of the points
        Vector3 centroid = Vector3.zero;
        for (int i = 0; i < n; i++) {
            centroid += points[i];
        }
        centroid /= n;

        // Compute the covariance matrix of the points
        Matrix4x4 cov = Matrix4x4.zero;
        for (int i = 0; i < n; i++) {
            Vector3 deviation = points[i] - centroid;
            cov[0, 0] += deviation.x * deviation.x;
            cov[0, 1] += deviation.x * deviation.y;
            cov[0, 2] += deviation.x * deviation.z;
            cov[1, 0] += deviation.y * deviation.x;
            cov[1, 1] += deviation.y * deviation.y;
            cov[1, 2] += deviation.y * deviation.z;
            cov[2, 0] += deviation.z * deviation.x;
            cov[2, 1] += deviation.z * deviation.y;
            cov[2, 2] += deviation.z * deviation.z;
        }
        for (int i = 0; i < 16; i++) cov[i] /= n;
        // cov /= n;

        // Compute the eigenvectors and eigenvalues of the covariance matrix
        Vector3[] eigenvectors = new Vector3[3];
        float[] eigenvalues = new float[3];
        EigenDecomposition(cov, out eigenvectors, out eigenvalues);

        // Sort the eigenvectors in descending order of their eigenvalues
        int[] sortedIndices = new int[] { 0, 1, 2 };
        System.Array.Sort(sortedIndices, (a, b) => eigenvalues[b].CompareTo(eigenvalues[a]));
        Vector3 firstDirection = eigenvectors[sortedIndices[0]];
        Vector3 secondDirection = eigenvectors[sortedIndices[1]];
        Vector3 thirdDirection = eigenvectors[sortedIndices[2]];

        // Construct a matrix that aligns the principal directions with the coordinate axes
        Matrix4x4 alignment = Matrix4x4.identity;
        alignment.SetColumn(0, firstDirection);
        alignment.SetColumn(1, secondDirection);
        alignment.SetColumn(2, thirdDirection);

        return (centroid, firstDirection, secondDirection, thirdDirection);
    }


    /// From ChatGPT (hope this works)
    /// This function computes the eigenvalues and eigenvectors 
    /// of a 3x3 matrix using the Jacobi method. 
    /// Note that the input matrix is assumed to be symmetric, 
    /// so there is no need to check for symmetry or to compute the 
    /// off-diagonal elements twice.
    ///
    /// The output consists of three eigenvectors (as Vector3 objects) 
    /// and three eigenvalues (as float values), sorted in descending 
    /// order of magnitude.
    public static void EigenDecomposition(Matrix4x4 mat, out Vector3[] eigenvectors, out float[] eigenvalues) {
        eigenvectors = new Vector3[3];
        eigenvalues = new float[3];

        // Initialize the eigenvectors to the identity matrix
        eigenvectors[0] = Vector3.right;
        eigenvectors[1] = Vector3.up;
        eigenvectors[2] = Vector3.forward;

        // Compute the off-diagonal elements of the matrix
        float a = mat[0, 1];
        float b = mat[0, 2];
        float c = mat[1, 2];

        // Compute the sum of the squares of the off-diagonal elements
        float sumsq = a * a + b * b + c * c;

        // Repeat until the off-diagonal elements are sufficiently small
        while (sumsq > 1e-15f) {
            // Compute the indices of the largest off-diagonal element
            int p, q;
            if (Mathf.Abs(a) > Mathf.Abs(b) && Mathf.Abs(a) > Mathf.Abs(c)) {
                p = 0;
                q = 1;
            } else if (Mathf.Abs(b) > Mathf.Abs(c)) {
                p = 0;
                q = 2;
            } else {
                p = 1;
                q = 2;
            }

            // Compute the rotation angle and sine/cosine values
            float theta = 0.5f * Mathf.Atan2(2 * mat[p, q], mat[p, p] - mat[q, q]);
            float sintheta = Mathf.Sin(theta);
            float costheta = Mathf.Cos(theta);

            // Construct the rotation matrix
            // Matrix3x3 rotmat = Matrix3x3.identity;
            Matrix4x4 rotmat = Matrix4x4.identity;
            rotmat[p, p] = costheta;
            rotmat[p, q] = -sintheta;
            rotmat[q, p] = sintheta;
            rotmat[q, q] = costheta;

            // Apply the rotation to the matrix and eigenvectors
            // mat = rotmat.Transpose() * mat * rotmat;
            mat = rotmat.transpose * mat * rotmat;
            eigenvectors[0] = rotmat * eigenvectors[0];
            eigenvectors[1] = rotmat * eigenvectors[1];
            eigenvectors[2] = rotmat * eigenvectors[2];

            // Compute the new off-diagonal elements and sum of squares
            a = mat[0, 1];
            b = mat[0, 2];
            c = mat[1, 2];
            sumsq = a * a + b * b + c * c;
        }

        // Copy the diagonal elements to the eigenvalue array
        eigenvalues[0] = mat[0, 0];
        eigenvalues[1] = mat[1, 1];
        eigenvalues[2] = mat[2, 2];

        // Sort the eigenvalues in decreasing order and permute the eigenvectors accordingly
        if (eigenvalues[0] < eigenvalues[1]) {
            Swap(ref eigenvalues[0], ref eigenvalues[1]);
            Swap(ref eigenvectors[0], ref eigenvectors[1]);
        }
        if (eigenvalues[1] < eigenvalues[2]) {
            Swap(ref eigenvalues[1], ref eigenvalues[2]);
            Swap(ref eigenvectors[1], ref eigenvectors[2]);
        }
        if (eigenvalues[0] < eigenvalues[1]) {
            Swap(ref eigenvalues[0], ref eigenvalues[1]);
            Swap(ref eigenvectors[0], ref eigenvectors[1]);
        }
    }

    private static void Swap<T>(ref T a, ref T b) {
        T temp = a;
        a = b;
        b = temp;
    }
}
