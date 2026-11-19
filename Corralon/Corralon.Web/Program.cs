namespace Corralon.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var app = builder.Build();

            app.UseHttpsRedirection();
            app.UseDefaultFiles();   // "/" -> wwwroot/index.html
            app.UseStaticFiles();

            app.Run();
        }
    }
}
