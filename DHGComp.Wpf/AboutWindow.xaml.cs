using System.Reflection;
using System.Windows;

namespace DHGComp;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        Assembly assembly = Assembly.GetExecutingAssembly();
        string title = assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? assembly.GetName().Name ?? "";

        Title = $"About {title}";
        txtProduct.Text = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? "";
        txtVersion.Text = $"Version: {assembly.GetName().Version}";
        txtCopyright.Text = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? "";
        txtCompany.Text = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? "";
        txtDescription.Text = (assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description ?? "") +
            "\r\n\r\n" +
            "Only allowed to use for personal, academic researches, or educational trainings." +
            "\r\n\r\n" +
            "DO NOT USE FOR COMMERCIAL PURPOSES!";
    }
}
