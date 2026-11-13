# DHGComp
Calculation of homology groups of 2D digital images

DHGComp is a Windows desktop program that computes the homology groups (ranks of
H0, H1, H2), the Betti numbers and the Euler characteristic of a 2D digital image.
It was developed by Ahmet ÖZTEL and Bayram AKGÜL at Bartın University.

## Citation

If you use DHGComp in your research, please cite the following article:

> Öztel, A., Akgül, B., Karaca, İ., Ege, Ö. Efficient computation of homology groups,
> Betti numbers, and Euler characteristics for 2D digital images.
> *Applicable Algebra in Engineering, Communication and Computing* **37**(3), 695–723 (2026).
> https://doi.org/10.1007/s00200-025-00682-w

```bibtex
@article{Oztel2026DHGComp,
  author  = {{\"O}ztel, Ahmet and Akg{\"u}l, Bayram and Karaca, {\.I}smet and Ege, {\"O}zg{\"u}r},
  title   = {Efficient computation of homology groups, {B}etti numbers, and {E}uler
             characteristics for {2D} digital images},
  journal = {Applicable Algebra in Engineering, Communication and Computing},
  volume  = {37},
  number  = {3},
  pages   = {695--723},
  year    = {2026},
  doi     = {10.1007/s00200-025-00682-w}
}
```

## What the program does

1. **Binary image.** The opened image (PNG, BMP, JPEG or GIF) is converted to black
   and white. A pixel is black (part of the object) when the average of its R, G and B
   values is below the mean of that average over the whole image; a uniform image is
   split at 128. Transparent pixels are treated as if the image lay on a white page.
2. **Digital simplicial complex** (8-adjacency):
   - 0-simplices: black pixels, numbered row by row,
   - 1-simplices: pairs of 8-adjacent black pixels,
   - 2-simplices: three black pixels of a 2×2 block,
   - 3-simplices: 2×2 blocks of four black pixels.
3. **Boundary operators** ∂1, ∂2 and ∂3 are built as integer matrices. Their ranks are
   computed exactly (fraction-free integer elimination, ranks over the rationals), and
   the ranks of the homology groups follow from the kernel and image dimensions:
   rank(Hq) = dim ker ∂q − dim im ∂(q+1).
4. **Euler characteristic** = #0-simplices − #1-simplices + #2-simplices − #3-simplices.

The toolbar offers three calculations:

| Button | Result |
|---|---|
| **Homology Groups** | ranks of H0, H1, H2, all kernel and image dimensions, simplex counts, Euler characteristic |
| **Only Betti Numbers** | Betti 0 and Betti 1 (needs only ∂1 and the Euler characteristic, so it is faster) |
| **Only Euler Characteristic** | simplex counts and the Euler characteristic |

With **Print Matrices** checked, the Homology Groups report also lists all simplices and
the boundary matrices before and after reduction (matrices with more than 4 million
cells are not printed).

The results are shown in the status bar and written to `<image name>_part.txt` next to
the program (or to `Documents\DHGComp` if that folder is not writable), which is then
opened in the default text editor. The window shows the original, grayscale, threshold
preview and binary images, and for images up to 64×64 pixels a grid with the
coordinates and vertex number of every black pixel.

## Requirements and building

- Windows 10 or 11 with the [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)
- To build: the .NET 10 SDK (or newer) or Visual Studio 2026

Open `DHGComp.Wpf/DHGComp.Wpf.csproj` in Visual Studio, or from the command line:

```
dotnet build DHGComp.Wpf/DHGComp.Wpf.csproj -c Release
dotnet run --project DHGComp.Wpf
```

An image can be opened with **Open Image** (Ctrl+O), by dropping the file onto the
window, or by passing its path on the command line (`DHGComp.exe image.png`).

## Changes in this version

The program was ported from Windows Forms on .NET Framework 4.5.2 to WPF on .NET 10.

- **Correctness**
  - Ranks: elimination with pivots other than ±1 used truncating integer division and
    could give a wrong rank. It is now exact.
  - Threshold: the image mean was truncated to an integer, so an almost entirely black
    image could end up with no black pixels at all, and a uniform black image had no
    0-simplices. Pixels exactly at the threshold were drawn black but left out of the complex.
  - The grayscale and threshold previews were wrong for image widths that are not a
    multiple of 4.
  - Fully transparent pixels counted as black, so a black shape on a transparent
    background became an all-black image.
- **Speed and memory.** Boundary matrices are now sparse and simplices are looked up
  in hash tables instead of by linear search. A completely black 20×20 image took 9.1 s
  and now takes 4 ms. A 64×64 image, which needed about 1 GB of dense matrices and was
  not feasible before, takes about 0.1 s; 128×128 takes about 0.3 s. The results are
  identical to those of the previous (corrected) algorithm.
- **User interface.** The calculation runs in the background, so the window keeps
  responding. The image file is no longer kept locked. The window follows the Windows
  light/dark theme, and the status bar shows the threshold and the number of black pixels.

## License

DHGComp is licensed under the [PolyForm Noncommercial License 1.0.0](LICENSE).

- You may use, modify and share it for any noncommercial purpose, such as research,
  teaching and personal study. Universities, public research organizations and
  government institutions may use it regardless of how they are funded.
- Commercial use is not permitted under this license. For commercial use, please
  contact the authors.
