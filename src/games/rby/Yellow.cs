public class Yellow : Rby {

    public Yellow() : base("roms/pokeyellow.gbc") { }

    public override void ChooseMenuItem(int target) {
        RunUntil("_Joypad", "HandleMenuInput_.getJoypadState");
        MenuScroll(target, Joypad.A, false);
    }

    public override void SelectMenuItem(int target) {
        RunUntil("_Joypad", "HandleMenuInput_.getJoypadState");
        MenuScroll(target, Joypad.Select, true);
    }

    public override void ChooseListItem(int target) {
        RunUntil("_Joypad", "HandleMenuInput_.getJoypadState");
        ListScroll(target, Joypad.A, false);
    }

    public override void SelectListItem(int target) {
        RunUntil("_Joypad", "HandleMenuInput_.getJoypadState");
        ListScroll(target, Joypad.Select, true);
    }
}
