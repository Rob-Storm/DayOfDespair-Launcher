namespace DoDLauncher.Model
{
    public class GameInstance
    {
        public string Name { get; set; }
        public string Version {  get; set; }
        public string ExecutablePath { get; set; }
        public string[] Arguments { get; set; }
        public bool Installed { get; set; } = false;
    }
}
