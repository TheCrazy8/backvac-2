using MelonLoader;

[assembly: MelonInfo(typeof(BackpackMod.BackpackMelonMod), "BackpackMod", "1.0.0", "TheCrazy8")]
[assembly: MelonGame("MonomiPark", "SlimeRancher2")]

namespace BackpackMod
{
    public class BackpackMelonMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("BackpackMod loaded! Press B to open your backpack.");
            BackpackUI.Init();
        }

        public override void OnUpdate()
        {
            BackpackUI.OnUpdate();
        }

        public override void OnGUI()
        {
            BackpackUI.DrawGUI();
        }
    }
}