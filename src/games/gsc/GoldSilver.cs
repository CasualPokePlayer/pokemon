public class GoldSilver : Gsc {

    public GoldSilver(string rom) : base(rom) { }
}

public class Gold : GoldSilver {

    public Gold() : base("roms/pokegold.gbc") { }
}

public class Silver : GoldSilver {

    public Silver() : base("roms/pokesilver.gbc") { }
}