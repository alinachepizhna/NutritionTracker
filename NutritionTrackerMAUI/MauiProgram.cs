using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Microcharts.Maui;
using SkiaSharp.Views.Maui.Controls.Hosting; 

namespace NutritionTrackerMAUI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMicrocharts() 
                .UseSkiaSharp()   
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            return builder.Build();
        }
    }
}