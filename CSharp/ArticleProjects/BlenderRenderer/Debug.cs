
namespace BlenderRenderer
{
     public static class Debug
    {
         public static void Log(string text)
         {
            Form1.FormIns.UpdateStatus(text);
         }
    }
}
