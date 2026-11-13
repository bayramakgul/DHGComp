// Calculation core carried over from the WinForms version (DHGCalc.cs in the repository root
// before the WinForms project was removed). Only the System.Drawing dependencies were removed: pixel coordinates are
// PixelPoint instead of System.Drawing.Point, and the binary image is no longer stored
// here. The numbers and the report text are the same.
#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DHGComp.Core
{
    public enum CalcType
    {
        Homology,
        Euler,
        Betti,
    }

    // Column (X) and row (Y) of a pixel.
    public readonly record struct PixelPoint(int X, int Y);

    public class DHGCalc
    {
        public string out_filename;
        private StringWriter output = null;
        public bool bPrintMatrix = false;

        public int[,] table;
        public int W, H;
        public List<PixelPoint> simp0_list = new List<PixelPoint>();
        public int KerDelta0, KerDelta1, KerDelta2, KerDelta3;
        public int ImDelta0, ImDelta1, ImDelta2, ImDelta3;
        public int H0, H1, H2;

        public void Init()
        {
            KerDelta0 = KerDelta1 = KerDelta2 = KerDelta3 =
            ImDelta0 = ImDelta1 = ImDelta2 = ImDelta3 =
            H0 = H1 = H2 = 0;

            simp0_list = new List<PixelPoint>();
        }

        // Builds the 0-simplices from a binary image (foreground[x, y] == true for black
        // pixels). Vertices are numbered row by row, left to right, as in the WinForms
        // version (FrmMain.StartProcessing), so the report and the pixel grid agree.
        public void SetImage(bool[,] foreground)
        {
            Init();
            W = foreground.GetLength(0);
            H = foreground.GetLength(1);
            table = new int[W, H];

            int count = 0;
            for (int j = 0; j < H; j++)
            {
                for (int i = 0; i < W; i++)
                {
                    table[i, j] = -1;
                    if (foreground[i, j])
                    {
                        table[i, j] = count++;
                        simp0_list.Add(new PixelPoint(i, j));
                        KerDelta0++;
                    }
                }
            }
        }

        public void StartCalc(CalcType calcType)
        {
            if (table == null || string.IsNullOrEmpty(out_filename))
                throw new InvalidOperationException("No image has been loaded.");

            List<string> Simp1 = Find_Simplex1_Adjacency();
            List<string> Simp2 = Find_Simplex2_Adjacency();
            List<string> Simp3 = Find_Simplex3_Adjacency();

            // The report is built in memory and written in one go, so an exception
            // can no longer leave the output file open (and locked).
            output = new StringWriter();

            int s0 = simp0_list.Count,
                    s1 = Simp1.Count,
                    s2 = Simp2.Count,
                    s3 = Simp3.Count;

            int Euler = (s0 - s1 + s2 - s3);

            switch (calcType)
            {
                case CalcType.Euler:
                    {
                        output.WriteLine($"# 0-Simplices: {s0}");
                        output.WriteLine($"# 1-Simplices: {s1}");
                        output.WriteLine($"# 2-Simplices: {s2}");
                        output.WriteLine($"# 3-Simplices: {s3}");

                        output.WriteLine($"Euler Characteristic = {Euler}");
                    }
                    break;
                case CalcType.Betti:
                    {
                        Boundary_Op1(Simp1);
                        H1 = H0 - Euler;

                        output.WriteLine($"Betti 0 = {H0}");
                        output.WriteLine($"Betti 1 = {H1}");
                        output.WriteLine();
                        output.WriteLine($"Euler Characteristic = {Euler}");
                        output.WriteLine();
                        output.WriteLine($"# 0-Simplices: {s0}");
                        output.WriteLine($"# 1-Simplices: {s1}");
                        output.WriteLine($"# 2-Simplices: {s2}");
                        output.WriteLine($"# 3-Simplices: {s3}");
                    }
                    break;
                case CalcType.Homology:
                    {
                        if (bPrintMatrix)
                        {
                            output.WriteLine($"***0-SIMPLEXES*******{simp0_list.Count}");
                            int cnt = 0;
                            foreach (PixelPoint p in simp0_list)
                                output.WriteLine($"{cnt++} :  x={p.X},  y={p.Y}");

                            output.WriteLine($"\r\n***1-SIMPLEXES*******{Simp1.Count}");
                            foreach (string s in Simp1)
                                output.WriteLine(s);

                            output.WriteLine($"\r\n***2-SIMPLEXES*******{Simp2.Count}");
                            foreach (string s in Simp2)
                                output.WriteLine(s);

                            output.WriteLine($"\r\n***3-SIMPLEXES*******{Simp3.Count}");
                            foreach (string s in Simp3)
                                output.WriteLine(s);

                            output.WriteLine();
                        }

                        Boundary_Op1(Simp1);
                        Boundary_Op2(Simp1, Simp2);
                        Boundary_Op3(Simp2, Simp3);

                        // Results go first, followed by the matrices written above.
                        string details = output.ToString();
                        output.GetStringBuilder().Clear();
                        PrintOut(s0, s1, s2, s3);
                        output.Write(details);
                    }
                    break;
            }

            string report = output.ToString();
            output.Dispose();
            output = null;
            File.WriteAllText(out_filename, report);

            // Showing the message and opening the report is left to the caller (MainWindow),
            // because the calculation now runs on a background thread.
        }

        string GetOrStr(int x, int y)
        {
            return x > y ? $"{x};{y}": $"{y};{x}";
        }

        public List<string> Find_Simplex1_Adjacency()
        {
            List<string> simp1 = new List<string>();
            for (int j = 0; j < H; j++)
            {
                for (int i = 0; i < W; i++)
                {
                    int px = table[i, j];
                    if (px < 0) continue;

                    if (i != W - 1)
                    {
                        int rg = table[i + 1, j];
                        if (rg >= 0)
                            simp1.Add(GetOrStr(px, rg));
                    }

                    if (j != H - 1)
                    {
                        int bt = table[i, j + 1];
                        if (bt >= 0)
                            simp1.Add(GetOrStr(px, bt));
                    }

                    if (!(i == 0 || j == H - 1))
                    {
                        int lfbt = table[i - 1, j + 1];
                        if (lfbt >= 0)
                            simp1.Add(GetOrStr(px, lfbt));
                    }

                    if (!(i == W - 1 || j == H - 1))
                    {
                        int rgbt = table[i + 1, j + 1];
                        if (rgbt >= 0)
                            simp1.Add(GetOrStr(px, rgbt));
                    }

                }
            }

            return simp1;
        }

        public List<string> Find_Simplex2_Adjacency()
        {
            List<string> simp2 = new List<string>();
            for (int j = 0; j < H; j++)
            {
                for (int i = 0; i < W; i++)
                {
                    int px = table[i, j];
                    if (px < 0) continue;


                    if (i != W - 1 && j != H - 1)
                    {
                        int rg = table[i + 1, j];
                        int bt = table[i, j + 1];

                        if (rg >= 0 && bt >= 0)
                            simp2.Add( $"{rg};{px};{bt}");
                    }

                    if (i != 0 && j != 0)
                    {
                        int lf = table[i - 1, j];
                        int tp = table[i, j - 1];

                        if (lf >= 0 && tp >= 0)
                            simp2.Add($"{lf};{px};{tp}");

                    }

                    if (i != W - 1 && j != 0)
                    {
                        int rg = table[i + 1, j];
                        int tp = table[i, j - 1];

                        if (rg >= 0 && tp >= 0)
                            simp2.Add($"{tp};{px};{rg}");
                    }

                    if (i != 0 && j != H - 1)
                    {
                        int lf = table[i - 1, j];
                        int bt = table[i, j + 1];

                        if (lf >= 0 && bt >= 0)
                            simp2.Add($"{bt};{px};{lf}");

                    }
                }
            }

            return simp2;
        }
        public List<string> Find_Simplex3_Adjacency()
        {
            List<string> simp3 = new List<string>();
            for (int j = 0; j < H - 1; j++)
            {
                for (int i = 0; i < W - 1; i++)
                {
                    int px = table[i, j];
                    if (px < 0) continue;

                    int rg = table[i + 1, j];
                    int bt = table[i, j + 1];
                    int rgbt = table[i + 1, j + 1];

                    if (rg >= 0 && bt >= 0 && rgbt >= 0)
                        simp3.Add($"{px};{bt};{rgbt};{rg}");
                }

            }
            return simp3;
        }

        //----------------------------
        // Boundary matrices are kept sparse: one row per simplex holding only its non-zero
        // (column, value) entries. A 64x64 image gives matrices of about 16000 x 16000;
        // as a dense int[,] that is ~1 GB, which a 32-bit process cannot allocate, and
        // dense elimination on it would take hours.

        public void Boundary_Op1(List<string> simp1_list)
        {
            var rows = new List<SparseRow>(simp1_list.Count);
            foreach (string str in simp1_list)
            {
                string[] ix = str.Split(';');
                int j1 = int.Parse(ix[0]);
                int j2 = int.Parse(ix[1]);

                rows.Add(MakeRow(new[] { j1, j2 }, new[] { -1, 1 }));
            }

            Reduce(rows, simp0_list.Count,
                "********** ∂1 ***********", "\r\n********** REDUCED ∂1 ***********",
                out KerDelta1, out ImDelta1);

            H1 = KerDelta1;
            H0 = KerDelta0 - ImDelta1;
        }
        public void Boundary_Op2(List<string> simp1_list, List<string> simp2_list)
        {
            // Edge ("larger;smaller") -> column; replaces a linear search over all edges
            // for every side of every triangle.
            var edgeIndex = new Dictionary<string, int>(simp1_list.Count);
            for (int j = 0; j < simp1_list.Count; j++)
                if (!edgeIndex.ContainsKey(simp1_list[j]))
                    edgeIndex.Add(simp1_list[j], j);

            var rows = new List<SparseRow>(simp2_list.Count);
            var cols = new List<int>(3);
            var vals = new List<int>(3);
            foreach (string str in simp2_list)
            {
                string[] ix = str.Split(';');
                int n0 = int.Parse(ix[0]);
                int n1 = int.Parse(ix[1]);
                int n2 = int.Parse(ix[2]);

                // ∂[n0,n1,n2] = [n1,n2] - [n0,n2] + [n0,n1]
                cols.Clear();
                vals.Clear();
                AddEdge(edgeIndex, n1, n2, 1, cols, vals);
                AddEdge(edgeIndex, n0, n2, -1, cols, vals);
                AddEdge(edgeIndex, n0, n1, 1, cols, vals);
                rows.Add(MakeRow(cols.ToArray(), vals.ToArray()));
            }

            Reduce(rows, simp1_list.Count,
                "\r\n********** ∂2 ***********", "\r\n********** REDUCED ∂2 ***********",
                out KerDelta2, out ImDelta2);

            H2 = KerDelta2;
            H1 = KerDelta1 - ImDelta2;
            H0 = KerDelta0 - ImDelta1;
        }
        public void Boundary_Op3(List<string> simp2_list, List<string> simp3_list)
        {
            // Triangle vertex set -> column; replaces comparing every tetrahedron with every triangle.
            var triangleIndex = new Dictionary<string, int>(simp2_list.Count);
            for (int j = 0; j < simp2_list.Count; j++)
            {
                string[] p = simp2_list[j].Split(';');
                string key = VertexSetKey(int.Parse(p[0]), int.Parse(p[1]), int.Parse(p[2]));
                if (!triangleIndex.ContainsKey(key))
                    triangleIndex.Add(key, j);
            }

            var rows = new List<SparseRow>(simp3_list.Count);
            var cols = new List<int>(4);
            var vals = new List<int>(4);
            var v = new int[4];
            foreach (string str in simp3_list)
            {
                string[] s = str.Split(';');
                for (int k = 0; k < 4; k++)
                    v[k] = int.Parse(s[k]);

                // The face without vertex k gets sign (-1)^k, as TestContains did.
                cols.Clear();
                vals.Clear();
                for (int k = 0; k < 4; k++)
                {
                    int a = v[k == 0 ? 1 : 0];
                    int b = v[k <= 1 ? 2 : 1];
                    int c = v[k <= 2 ? 3 : 2];
                    int j;
                    if (triangleIndex.TryGetValue(VertexSetKey(a, b, c), out j))
                    {
                        cols.Add(j);
                        vals.Add(k % 2 == 0 ? 1 : -1);
                    }
                }
                rows.Add(MakeRow(cols.ToArray(), vals.ToArray()));
            }

            Reduce(rows, simp2_list.Count,
                "\r\n********** ∂3 ***********", "\r\n********** REDUCED ∂3 ***********",
                out KerDelta3, out ImDelta3);

            H2 = KerDelta2 - ImDelta3;
        }

        // Matrices up to this many cells are still printed in full with "Print Matrices".
        private const long MaxPrintCells = 4000000;

        // Kernel and image dimensions of a boundary operator (rows = simplices).
        // With "Print Matrices" the matrix is expanded to the dense form and reduced exactly
        // as before, so the printed report is unchanged; otherwise the rank comes from the
        // sparse elimination.
        private void Reduce(List<SparseRow> rows, int columnCount, string title, string reducedTitle,
            out int ker, out int im)
        {
            if (bPrintMatrix && (long)rows.Count * columnCount <= MaxPrintCells)
            {
                int[,] dense = ToDense(rows, columnCount);
                PrintMatrix(dense, title);
                int[,] arr = SmithPoincere(dense);
                ker = Calc_Kernel_Delta(arr);
                im = Calc_Image_Delta(arr);
                PrintMatrix(arr, reducedTitle);
                return;
            }

            if (bPrintMatrix)
                output.WriteLine($"{title}\r\n({rows.Count} x {columnCount}: too large to print)");

            int rank = SparseRank(rows, columnCount);
            ker = rows.Count - rank;
            im = rank;
        }

        private sealed class SparseRow
        {
            public readonly int[] Cols;   // ascending column indices
            public readonly int[] Vals;   // matching non-zero values

            public SparseRow(int[] cols, int[] vals)
            {
                Cols = cols;
                Vals = vals;
            }

            public int Count { get { return Cols.Length; } }
        }

        private static SparseRow MakeRow(int[] cols, int[] vals)
        {
            Array.Sort(cols, vals);
            return new SparseRow(cols, vals);
        }

        // Adds the oriented edge [a,b] with the given coefficient. Edges are stored as
        // "larger;smaller", i.e. oriented from the larger to the smaller index.
        private static void AddEdge(Dictionary<string, int> edgeIndex, int a, int b, int coefficient,
            List<int> cols, List<int> vals)
        {
            int j;
            if (edgeIndex.TryGetValue(a > b ? $"{a};{b}" : $"{b};{a}", out j))
            {
                cols.Add(j);
                vals.Add(a > b ? coefficient : -coefficient);
            }
        }

        private static string VertexSetKey(int a, int b, int c)
        {
            int t;
            if (a > b) { t = a; a = b; b = t; }
            if (b > c) { t = b; b = c; c = t; }
            if (a > b) { t = a; a = b; b = t; }
            return $"{a};{b};{c}";
        }

        private static int[,] ToDense(List<SparseRow> rows, int columnCount)
        {
            int[,] m = new int[rows.Count, columnCount];
            for (int i = 0; i < rows.Count; i++)
                for (int k = 0; k < rows[i].Count; k++)
                    m[i, rows[i].Cols[k]] = rows[i].Vals[k];
            return m;
        }

        // Rank over Q by exact, fraction-free forward elimination on sparse rows. Each row is
        // reduced against the pivot rows found so far (keyed by their leading column) until it
        // vanishes (linearly dependent) or has a leading column without a pivot (new pivot).
        private static int SparseRank(List<SparseRow> rows, int columnCount)
        {
            var pivots = new SparseRow[columnCount];
            int rank = 0;
            foreach (SparseRow start in rows)
            {
                SparseRow row = start;
                while (row.Count > 0)
                {
                    SparseRow pivot = pivots[row.Cols[0]];
                    if (pivot == null)
                    {
                        pivots[row.Cols[0]] = row;
                        rank++;
                        break;
                    }
                    row = Eliminate(row, pivot);
                }
            }
            return rank;
        }

        // a * row - b * pivot with the leading entries cancelling, divided by the gcd of its entries.
        private static SparseRow Eliminate(SparseRow row, SparseRow pivot)
        {
            int g = Gcd(row.Vals[0], pivot.Vals[0]);
            int a = pivot.Vals[0] / g;
            int b = row.Vals[0] / g;

            int[] cols = new int[row.Count + pivot.Count];
            int[] vals = new int[cols.Length];
            int i = 1, j = 1, n = 0;
            while (i < row.Count || j < pivot.Count)
            {
                int c, v;
                if (j >= pivot.Count || (i < row.Count && row.Cols[i] < pivot.Cols[j]))
                {
                    c = row.Cols[i];
                    v = checked(a * row.Vals[i]);
                    i++;
                }
                else if (i >= row.Count || pivot.Cols[j] < row.Cols[i])
                {
                    c = pivot.Cols[j];
                    v = checked(-(b * pivot.Vals[j]));
                    j++;
                }
                else
                {
                    c = row.Cols[i];
                    v = checked(a * row.Vals[i] - b * pivot.Vals[j]);
                    i++;
                    j++;
                }

                if (v != 0)
                {
                    cols[n] = c;
                    vals[n] = v;
                    n++;
                }
            }

            int d = 0;
            for (int k = 0; k < n && d != 1; k++)
                d = Gcd(d, vals[k]);
            if (d > 1)
                for (int k = 0; k < n; k++)
                    vals[k] /= d;

            Array.Resize(ref cols, n);
            Array.Resize(ref vals, n);
            return new SparseRow(cols, vals);
        }
        //----------------------------

        public int Calc_Kernel_Delta(int[,] arr)
        {
            int row = arr.GetLength(0);
            int clm = arr.GetLength(1);

            int KerDelta = 0;
            for (int i = 0; i < row; i++)
            {
                bool zero = true;
                for (int j = 0; j < clm; j++)
                {
                    if (arr[i, j] != 0)
                    {
                        zero = false;
                        break;
                    }
                }

                if (zero)
                    KerDelta++;
            }

            return KerDelta;
        }

        public int Calc_Image_Delta(int[,] arr)
        {
            int row = arr.GetLength(0);
            int clm = arr.GetLength(1);

            int ImDelta = 0;
            for (int j = 0; j < clm; j++)
            {
                bool non_zero = false;
                for (int i = 0; i < row; i++)
                {
                    if (arr[i, j] != 0)
                    {
                        non_zero = true;
                        break;
                    }
                }

                if (non_zero)
                    ImDelta++;
            }

            return ImDelta;
        }

        void PrintMatrix(int[,] arr, string info)
        {
            output.WriteLine(info);
            int icnt = arr.GetLength(0);
            int jcnt = arr.GetLength(1);

            var text = new StringBuilder();
            for (int i = 0; i < icnt; i++)
            {
                text.Clear();
                for (int j = 0; j < jcnt; j++)
                {
                    text.Append(arr[i, j]).Append(' ');
                }
                output.WriteLine(text.ToString());
            }

        }

        private void PrintOut(int s0, int s1, int s2, int s3)
        {

            output.WriteLine("\r\n***RESULTS****");

            output.WriteLine($"Rank(H0):{H0}");
            output.WriteLine($"Rank(H1):{H1}");
            output.WriteLine($"Rank(H2):{H2}");
            output.WriteLine();

            //****
            output.WriteLine($"Euler Characteristic = {(s0 - s1 + s2 - s3)}");
            output.WriteLine($"# 0-Simplices: {s0}");
            output.WriteLine($"# 1-Simplices: {s1}");
            output.WriteLine($"# 2-Simplices: {s2}");
            output.WriteLine($"# 3-Simplices: {s3}");
            output.WriteLine();
            //****

            output.WriteLine($"Rank(KerDelta0):{KerDelta0}");
            output.WriteLine($"Rank(KerDelta1):{KerDelta1}");
            output.WriteLine($"Rank(KerDelta2):{KerDelta2}");
            output.WriteLine($"Rank(KerDelta3):{KerDelta3}");
            output.WriteLine();
            output.WriteLine($"Rank(ImDelta0):{ImDelta0}");
            output.WriteLine($"Rank(ImDelta1):{ImDelta1}");
            output.WriteLine($"Rank(ImDelta2):{ImDelta2}");
            output.WriteLine($"Rank(ImDelta3):{ImDelta3}");

            output.WriteLine("************************************************\r\n");
        }

        // Not a full Smith normal form: two exact Gauss-Jordan passes leave a matrix whose
        // zero rows / non-zero columns give the kernel and image dimensions (ranks over Q).
        private  int[,] SmithPoincere(int[,] matrix)
        {
            int[,] a = Eshelon(matrix);
            int[,] b = Transpose(a);
            int[,] c = Eshelon(b);
            int[,] d = Transpose(c);
            return d;
        }

        private  int[,] Eshelon(int[,] matrix)
        {
            int lead = 0,
                rowCount = matrix.GetLength(0),
                columnCount = matrix.GetLength(1);

            for (int r = 0; r < rowCount; r++)
            {
                if (columnCount <= lead) break;
                int i = r;
                while (matrix[i, lead] == 0)
                {
                    i++;
                    if (i == rowCount)
                    {
                        i = r;
                        lead++;
                        if (columnCount == lead)
                        {
                            lead--;
                            break;
                        }
                    }
                }
                for (int j = 0; j < columnCount; j++)
                {
                    int temp = matrix[r, j];
                    matrix[r, j] = matrix[i, j];
                    matrix[i, j] = temp;
                }

                // Exact integer elimination. With a pivot of +-1 this is the same as before.
                // Other pivots used to be handled with integer division, which truncated
                // (e.g. 1 / 2 == 0) and could give a wrong rank; rows are now combined
                // fraction-free instead: row_j = a * row_j - b * row_r.
                if (matrix[r, lead] != 0)
                {
                    ReduceRow(matrix, r);
                    int div = matrix[r, lead];
                    if (div == -1)
                    {
                        for (int j = 0; j < columnCount; j++)
                            matrix[r, j] = -matrix[r, j];
                        div = 1;
                    }

                    for (int j = 0; j < rowCount; j++)
                    {
                        if (j == r) continue;
                        int sub = matrix[j, lead];
                        if (sub == 0) continue;

                        if (div == 1)
                        {
                            for (int k = 0; k < columnCount; k++)
                                matrix[j, k] = checked(matrix[j, k] - sub * matrix[r, k]);
                        }
                        else
                        {
                            int g = Gcd(div, sub);
                            int a = div / g, b = sub / g;
                            for (int k = 0; k < columnCount; k++)
                                matrix[j, k] = checked(a * matrix[j, k] - b * matrix[r, k]);
                            ReduceRow(matrix, j);
                        }
                    }
                }
                lead++;
            }
            return matrix;
        }

        private static int Gcd(int a, int b)
        {
            a = Math.Abs(a);
            b = Math.Abs(b);
            while (b != 0)
            {
                int t = a % b;
                a = b;
                b = t;
            }
            return a;
        }

        // Divides a row by the gcd of its entries (keeps numbers small, does not change the rank).
        private static void ReduceRow(int[,] matrix, int row)
        {
            int columnCount = matrix.GetLength(1);
            int g = 0;
            for (int k = 0; k < columnCount && g != 1; k++)
                if (matrix[row, k] != 0)
                    g = Gcd(g, matrix[row, k]);

            if (g > 1)
                for (int k = 0; k < columnCount; k++)
                    matrix[row, k] /= g;
        }


        private  int[,] Transpose(int[,] matrix)
        {
            int w = matrix.GetLength(0);
            int h = matrix.GetLength(1);

            int[,] result = new int[h, w];

            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    result[j, i] = matrix[i, j];
                }
            }

            return result;
        }
    }
}
