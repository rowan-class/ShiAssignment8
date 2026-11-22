using Gum.Forms.Controls;
using Gum.Wireframe;

namespace GameBase;

public class GameSceneUI : SceneUI
{
    Button activeSlot;
    Label passiveSlot;

    public GameSceneUI(GameScene s) : base(s) { }

    protected override void Init()
    {
        activeSlot = new Button();
        passiveSlot = new Label();
        Root.AddChild(activeSlot);
        Root.AddChild(passiveSlot);

        activeSlot.Anchor(Anchor.Bottom);
        passiveSlot.Anchor(Anchor.Bottom);
        activeSlot.X = -80;
        passiveSlot.X = 80;
        activeSlot.Y = passiveSlot.Y = -8;
        activeSlot.Width = 145;
        activeSlot.Height = 32;
        activeSlot.Click += (sender, args) => ((GameScene)scene).UseItem();
    }

    public void UpdateSlots(string active, string passive, int cooldown, bool canUse)
    {
        activeSlot.Text = active;
        if (cooldown > 0) { activeSlot.Text += " " + cooldown; }
        activeSlot.IsEnabled = canUse && cooldown == 0;
        passiveSlot.Text = "passive  " + passive;
    }
}
