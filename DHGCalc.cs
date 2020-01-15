using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace DHGComp1
{
    public enum CalcType
    {
        Homology,
        Euler,
        Betti,
    }

    public class DHGCalc
    {
        public string out_filename;
        public StreamWriter output = null;
        public bool bPrintMatrix = false;

        public Bitmap bit;
        public int[,] table;
        public int W, H;
        public List<Point> simp0_list = new List<Point>();
        public int KerDelta0, KerDelta1, KerDelta2, KerDelta3;
        public int ImDelta0, ImDelta1, ImDelta2, ImDelta3;
        public int H0, H1, H2;

        public void Init()
        {
            KerDelta0 = KerDelta1 = KerDelta2 = KerDelta3 =
            ImDelta0 = ImDelta1 = ImDelta2 = ImDelta3 =
            H0 = H1 = H2 = 0;

            simp0_list = new List<Point>();
        }

        public void StartCalc(CalcType calcType)
        {

            List<string> Simp1 = Find_Simplex1_Adjacency();
            List<string> Simp2 = Find_Simplex2_Adjacency();
            List<string> Simp3 = Find_Simplex3_Adjacency();

            output = new StreamWriter(out_filename);

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

                        output.WriteLine($"Euler Charcteristic = {Euler}");

                        output.Flush();
                        output.Close();
                    }
                    break;
                case CalcType.Betti:
                    {
                        Boundary_Op1(Simp1);
                        H1 = H0 - Euler;

                        output.WriteLine($"Betti 0 = {H0}");
                        output.WriteLine($"Betti 1 = {H1}");
                        output.WriteLine();
                        output.WriteLine($"Euler Charcteristic = {Euler}");
                        output.WriteLine();
                        output.WriteLine($"# 0-Simplices: {s0}");
                        output.WriteLine($"# 1-Simplices: {s1}");
                        output.WriteLine($"# 2-Simplices: {s2}");
                        output.WriteLine($"# 3-Simplices: {s3}");

                        output.Flush();
                        output.Close();
                    }
                    break;
                case CalcType.Homology:
                    {
                        if (bPrintMatrix)
                        {
                            output.WriteLine($"***0-SIMPLEXES*******{simp0_list.Count}");
                            int cnt = 0;
                            foreach (Point p in simp0_list)
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

                        output.Flush();
                        output.Close();

                        StreamReader sreader = new StreamReader(out_filename);
                        {
                            string str = sreader.ReadToEnd();
                            sreader.Close();

                            output = new StreamWriter(out_filename);
                            PrintOut(s0, s1, s2, s3);
                            output.Write(str);
                            output.Flush();
                            output.Close();
                        }
                    }
                    break;
            }

            System.Diagnostics.Process.Start(out_filename);
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

        public void Boundary_Op1(List<string> simp1_list)
        {
            int row = simp1_list.Count;
            int clm = simp0_list.Count;

            int[,] BoundOp1 = new int[row, clm];

            for (int i = 0; i < row; i++)
            {
                string str = simp1_list[i];
                string[] ix = str.Split(new char[] { ';' });

                int j1 = int.Parse(ix[0]);
                int j2 = int.Parse(ix[1]);

                BoundOp1[i, j1] = -1;
                BoundOp1[i, j2] = 1;
            }

            //*****************************************************************
            if (bPrintMatrix) PrintMatrix(BoundOp1, "********** ∂1 ***********");
            int[,] arr = SmithPoincere(BoundOp1);
            KerDelta1 = Calc_Kernel_Delta(arr);
            ImDelta1 = Calc_Image_Delta(arr);
            if (bPrintMatrix) PrintMatrix(arr, "\r\n********** REDUCED ∂1 ***********");
            //**********************************************************************
            H1 = KerDelta1;
            H0 = KerDelta0 - ImDelta1;
        }
        public void Boundary_Op2(List<string> simp1_list, List<string> simp2_list)
        {
            int row = simp2_list.Count;
            int clm = simp1_list.Count;

            int[,] BoundOp2 = new int[row, clm];
            for (int i = 0; i < row; i++)
            {
                string str = simp2_list[i];
                string[] ix = str.Split(new char[] { ';' });
                int n0 = int.Parse(ix[0]);
                int n1 = int.Parse(ix[1]);
                int n2 = int.Parse(ix[2]);

                int sign = 1;
                string bi = $"{ix[1]};{ix[2]}";
                if (n2 > n1)
                {
                    sign = -1;
                    bi = $"{ix[2]};{ix[1]}";
                }

                for (int j = 0; j < simp1_list.Count; j++)
                {
                    if (bi == simp1_list[j]) BoundOp2[i, j] = sign;
                }

                //-------------
                sign = -1;
                string si = $"{ix[0]};{ix[2]}";
                if (n2 > n0)
                {
                    sign = 1;
                    si = $"{ix[2]};{ix[0]}";
                }

                for (int j = 0; j < simp1_list.Count; j++)
                {
                    if (si == simp1_list[j]) BoundOp2[i, j] = sign;
                }

                //-------------
                sign = 1;
                string sb = $"{ix[0]};{ix[1]}";
                if (n1 > n0)
                {
                    sign = -1;
                    sb = $"{ix[1]};{ix[0]}";
                }

                for (int j = 0; j < simp1_list.Count; j++)
                {
                    if (sb == simp1_list[j]) BoundOp2[i, j] = sign;
                }

            }

            //*********************************************************************
            if (bPrintMatrix) PrintMatrix(BoundOp2, "\r\n********** ∂2 ***********");
            int[,] arr = SmithPoincere(BoundOp2);
            KerDelta2 = Calc_Kernel_Delta(arr);
            ImDelta2 = Calc_Image_Delta(arr);

            if (bPrintMatrix) PrintMatrix(arr, "\r\n********** REDUCED ∂2 ***********");
            H2 = KerDelta2;
            H1 = KerDelta1 - ImDelta2;
            H0 = KerDelta0 - ImDelta1;

            //********************************************************************
        }
        public void Boundary_Op3(List<string> simp2_list, List<string> simp3_list)
        {
            int row = simp3_list.Count;
            int clm = simp2_list.Count;

            int[,] BoundOp3 = new int[row, clm];
            for (int i = 0; i < row; i++)
            {
                string source = simp3_list[i];
                for (int j = 0; j < clm; j++)
                {
                    string path = simp2_list[j];
                    BoundOp3[i, j] = TestContains(source, path);
                }
            }

            //********************************************************************
            if (bPrintMatrix) PrintMatrix(BoundOp3, "\r\n********** ∂3 ***********");
            int[,] arr = SmithPoincere(BoundOp3);
            KerDelta3 = Calc_Kernel_Delta(arr);
            ImDelta3 = Calc_Image_Delta(arr);

            if (bPrintMatrix) PrintMatrix(arr, "\r\n********** REDUCED ∂3 ***********");
            H2 = KerDelta2 - ImDelta3;
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

            for (int i = 0; i < icnt; i++)
            {
                string text = "";
                for (int j = 0; j < jcnt; j++)
                {
                    text += (arr[i, j] + " ");
                }
                output.WriteLine(text);
            }

        }

        int TestContains(string sourse, string path)
        {
            string[] s = sourse.Split(new char[] { ';' });
            string[] p = path.Split(new char[] { ';' });


            for (int i = 0; i < p.Length; i++)
                if (!s.Contains(p[i]))
                    return 0;

            if (!p.Contains(s[0])) return 1;
            if (!p.Contains(s[1])) return -1;
            if (!p.Contains(s[2])) return 1;
            if (!p.Contains(s[3])) return -1;
            return 1;
        }

        private void PrintOut(int s0, int s1, int s2, int s3)
        {

            output.WriteLine("\r\n***RESULTS****");

            output.WriteLine($"Rank(H0):{H0}");
            output.WriteLine($"Rank(H1):{H1}");
            output.WriteLine($"Rank(H2):{H2}");
            output.WriteLine();

            //****
            output.WriteLine($"Euler Charcteristic = {(s0 - s1 + s2 - s3)}");
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

            MessageBox.Show("Process completed!", "Process", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

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
                int div = matrix[r, lead];
                if (div != 0)
                    for (int j = 0; j < columnCount; j++) 
                        matrix[r, j] /= div;
                for (int j = 0; j < rowCount; j++)
                {
                    if (j != r)
                    {
                        int sub = matrix[j, lead];
                        for (int k = 0; k < columnCount; k++) 
                            matrix[j, k] -= (sub * matrix[r, k]);
                    }
                }
                lead++;
            }
            return matrix;
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
