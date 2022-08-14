using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class ReadBlueTID
{
    private const Joypad NO_INPUT = Joypad.None;
    private const Joypad A = Joypad.A;
    private const Joypad B = Joypad.B;
    private const Joypad SELECT = Joypad.Select;
    private const Joypad START = Joypad.Start;
    private const Joypad UP = Joypad.Up;
    private const Joypad DOWN = Joypad.Down;

    /* Change this to "blue" or "red" before running */
    private const string gameName = "red";

    /* Generate standard TIDs, or ones that include options. Opts mode is annoying, so max cost is lower. */
    private const bool optsMode = true;

    /* Change this to increase/decrease number of intro sequence combinations processed */
    private const int MAX_COST = optsMode ? 2350 : 2750;

    private const int TITLE_BASE_COST = gameName == "blue" ? 0 : 1;
    private const int CRY_BASE_COST = gameName == "blue" ? 96 : 88;

    private static class RedBlueAddr
    {
        // All ROM addresses used so far are identical for Red & Blue
        public const int biosReadKeypadAddr = 0x021D;
        public const int joypadAddr = 0x019A;
        public const int postTIDAddr = 0x037860;
        public const int animateNidorinoAddr = 0x105793;
        public const int checkInterruptAddr = 0x12F8;
        public const int joypadOverworldAddr = 0x0F4D;
        public const int printLetterDelayAddr = 0x38D3;
        public const int newBattleAddr = 0x0683;
        public const int delayAtEndOfShootingStarAddr = 0x1058CB;
        public const int textJingleCommandAddr = 0x1C31;
        public const int enterMapAddr = 0x03A6;
        public const int encounterTestAddr = 0x0478C4;
        public const int igtInjectAddr = 0x1C766A;
        public const int npcTimerExpireAddr = 0x0151FD;
        public const int catchSuccessAddr = 0x035868;
        public const int catchFailureAddr = 0x035922;
        public const int manualTextScrollAddr = 0x3898;
        public const int playCryAddr = 0x13D0;
        public const int displayListMenuIdAddr = 0x2BE6;
        public const int softResetAddr = 0x1F49;
        public const int displayTitleScreenAddr = 0x0142DD;
        public const int titleScreenPickNewMonAddr = 0x014496;
        public const int initAddr = 0x1F54;
        public const int displayTextBoxIdAddr = 0x30E8;
        public const int displayNamingScreenAddr = 0x016596;
        public const int calcStatAddr = 0x394A;
        public const int updateNpcSpriteAddr = 0x014ED1;
    }

    private class Strat
    {
        public readonly string name;
        public readonly int cost;
        protected readonly int[] addr;
        protected readonly Joypad[] input;
        protected readonly int[] advanceFrames;

        public Strat(string name, int cost, int[] addr, Joypad[] input, int[] advanceFrames)
        {
            this.addr = addr;
            this.cost = cost;
            this.name = name;
            this.input = input;
            this.advanceFrames = advanceFrames;
        }

        public virtual void Execute(Rby gb)
        {
            for (int i = 0; i < addr.Length; i++)
            {
                gb.RunUntil(addr[i]);
                gb.Inject(input[i]);
                gb.AdvanceFrames(advanceFrames[i]);
            }
        }
    }

    private class ResetStrat : Strat
    {
        public ResetStrat(string name, int cost, int[] addr, Joypad[] input, int[] advanceFrames)
            : base(name, cost, addr, input, advanceFrames)
        {
        }

        public override void Execute(Rby gb)
        {
            base.Execute(gb);
            gb.Hold(A | B | START | SELECT, RedBlueAddr.softResetAddr);
        }
    }

    private static readonly Strat gfSkip =
        new Strat("_gfskip", 0,
        new int[] { RedBlueAddr.joypadAddr },
        new Joypad[] { UP | SELECT | B, },
        new int[] { 1 });

    private static readonly Strat gfWait =
        new Strat("_gfwait", 253,
        new int[] { RedBlueAddr.delayAtEndOfShootingStarAddr },
        new Joypad[] { NO_INPUT },
        new int[] { 0 });

    private static readonly List<Strat> gf = new List<Strat>(new[] { gfSkip, gfWait });

    private static readonly Strat nido0 =
        new Strat("_hop0", 172 + TITLE_BASE_COST,
        new int[] { RedBlueAddr.joypadAddr },
        new Joypad[] { UP | SELECT | B },
        new int[] { 1 });

    private static readonly Strat nido1 =
        new Strat("_hop1", 172 + 131 + TITLE_BASE_COST,
        new int[] { RedBlueAddr.animateNidorinoAddr, RedBlueAddr.checkInterruptAddr, RedBlueAddr.joypadAddr },
        new Joypad[] { NO_INPUT, NO_INPUT, UP | SELECT | B },
        new int[] { 0, 0, 1 });

    private static readonly Strat nido2 =
        new Strat("_hop2", 172 + 190 + TITLE_BASE_COST,
        new int[] { RedBlueAddr.animateNidorinoAddr, RedBlueAddr.checkInterruptAddr, RedBlueAddr.animateNidorinoAddr, RedBlueAddr.checkInterruptAddr, RedBlueAddr.joypadAddr },
        new Joypad[] { NO_INPUT, NO_INPUT, NO_INPUT, NO_INPUT, UP | SELECT | B },
        new int[] { 0, 0, 0, 0, 1 });

    private static readonly Strat nido3 =
        new Strat("_hop3", 172 + 298 + TITLE_BASE_COST,
        new int[] { RedBlueAddr.animateNidorinoAddr, RedBlueAddr.checkInterruptAddr, RedBlueAddr.animateNidorinoAddr, RedBlueAddr.checkInterruptAddr, RedBlueAddr.animateNidorinoAddr, RedBlueAddr.checkInterruptAddr, RedBlueAddr.joypadAddr },
        new Joypad[] { NO_INPUT, NO_INPUT, NO_INPUT, NO_INPUT, NO_INPUT, NO_INPUT, UP | SELECT | B },
        new int[] { 0, 0, 0, 0, 0, 0, 1 });

    private static readonly Strat nido4 =
        new Strat("_hop4", 172 + 447 + TITLE_BASE_COST,
        new int[] { RedBlueAddr.animateNidorinoAddr, RedBlueAddr.checkInterruptAddr, RedBlueAddr.animateNidorinoAddr, RedBlueAddr.checkInterruptAddr, RedBlueAddr.animateNidorinoAddr, RedBlueAddr.checkInterruptAddr, RedBlueAddr.animateNidorinoAddr, RedBlueAddr.checkInterruptAddr, RedBlueAddr.joypadAddr },
        new Joypad[] { NO_INPUT, NO_INPUT, NO_INPUT, NO_INPUT, NO_INPUT, NO_INPUT, NO_INPUT, NO_INPUT, UP | SELECT | B },
        new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 1 });

    private static readonly Strat nido5 =
        new Strat("_hop5", 172 + 487 + TITLE_BASE_COST,
        new int[] { RedBlueAddr.animateNidorinoAddr, RedBlueAddr.checkInterruptAddr, RedBlueAddr.animateNidorinoAddr, RedBlueAddr.checkInterruptAddr, RedBlueAddr.animateNidorinoAddr, RedBlueAddr.checkInterruptAddr, RedBlueAddr.animateNidorinoAddr, RedBlueAddr.checkInterruptAddr, RedBlueAddr.animateNidorinoAddr, RedBlueAddr.checkInterruptAddr, RedBlueAddr.joypadAddr },
        new Joypad[] { NO_INPUT, NO_INPUT, NO_INPUT, NO_INPUT, NO_INPUT, NO_INPUT, NO_INPUT, NO_INPUT, NO_INPUT, NO_INPUT, UP | SELECT | B },
        new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 });

    private static readonly Strat nido6 =
        new Strat("_hop6", 172 + 536 + TITLE_BASE_COST,
        new int[] { RedBlueAddr.displayTitleScreenAddr },
        new Joypad[] { NO_INPUT },
        new int[] { 0 });

    private static readonly List<Strat> nido = new List<Strat>(new[] { nido0, nido1, nido2, nido3, nido4, nido5, nido6 });

    private static readonly Strat newGame =
        new Strat("_newgame", 20 + 20,
        new int[] { RedBlueAddr.joypadAddr },
        new Joypad[] { A },
        new int[] { 32 });

    private static readonly Strat backout =
        new Strat("_backout", 142 + 20 + TITLE_BASE_COST,
        new int[] { RedBlueAddr.joypadAddr },
        new Joypad[] { B },
        new int[] { 1 });

    private static readonly Strat options =
        new Strat("_opt", 24,
        new int[] { RedBlueAddr.joypadAddr },
        new Joypad[] { DOWN | A },
        new int[] { 1 });

    private static readonly Strat optback =
        new Strat("(backout)", 48,
        new int[] { RedBlueAddr.joypadAddr },
        new Joypad[] { START },
        new int[] { 1 });

    private static readonly ResetStrat gfReset =
        new ResetStrat("_gfreset", 363,
        new int[] { RedBlueAddr.joypadAddr },
        new Joypad[] { A | B | START | SELECT },
        new int[] { 0 });

    private static readonly ResetStrat hop0Reset =
        new ResetStrat("_hop0(reset)", 363 + 4,
        new int[] { RedBlueAddr.joypadAddr },
        new Joypad[] { A | B | START | SELECT },
        new int[] { 0 });

    private static readonly ResetStrat ngReset =
        new ResetStrat("_ngreset", 363,
        new int[] { RedBlueAddr.joypadAddr },
        new Joypad[] { A | B | START | SELECT },
        new int[] { 0 });

    private static readonly ResetStrat optReset =
        new ResetStrat("_opt(reset)", 363 + 24,
        new int[] { RedBlueAddr.joypadAddr },
        new Joypad[] { DOWN | A },
        new int[] { 0 });

    private static readonly ResetStrat oakReset =
        new ResetStrat("_oakreset", 363 + 114,
        new int[] { RedBlueAddr.joypadAddr },
        new Joypad[] { A | B | START | SELECT },
        new int[] { 0 });

    private static readonly ResetStrat hop1Reset =
        new ResetStrat("_hop1(reset)", 363 + 131 + 3,
        new int[] { RedBlueAddr.animateNidorinoAddr, RedBlueAddr.checkInterruptAddr, RedBlueAddr.joypadAddr },
        new Joypad[] { NO_INPUT, NO_INPUT, A | B | START | SELECT },
        new int[] { 0, 0, 0 });

    private static readonly ResetStrat hop2Reset =
        new ResetStrat("_hop2(reset)", 363 + 190 + 4,
        new int[] { RedBlueAddr.animateNidorinoAddr, RedBlueAddr.checkInterruptAddr, RedBlueAddr.animateNidorinoAddr, RedBlueAddr.checkInterruptAddr, RedBlueAddr.joypadAddr },
        new Joypad[] { NO_INPUT, NO_INPUT, NO_INPUT, NO_INPUT, A | B | START | SELECT },
        new int[] { 0, 0, 0, 0, 0 });

    private static readonly ResetStrat hop3Reset =
        new ResetStrat("_hop3(reset)", 363 + 298 + 5,
        new int[] { RedBlueAddr.animateNidorinoAddr, RedBlueAddr.checkInterruptAddr, RedBlueAddr.animateNidorinoAddr, RedBlueAddr.checkInterruptAddr, RedBlueAddr.animateNidorinoAddr, RedBlueAddr.checkInterruptAddr, RedBlueAddr.joypadAddr },
        new Joypad[] { NO_INPUT, NO_INPUT, NO_INPUT, NO_INPUT, NO_INPUT, NO_INPUT, A | B | START | SELECT },
        new int[] { 0, 0, 0, 0, 0, 0, 0 });

    private static readonly ResetStrat hop4Reset =
        new ResetStrat("_hop4(reset)", 363 + 447 + 4,
        new int[] { RedBlueAddr.animateNidorinoAddr, RedBlueAddr.checkInterruptAddr, RedBlueAddr.animateNidorinoAddr, RedBlueAddr.checkInterruptAddr, RedBlueAddr.animateNidorinoAddr, RedBlueAddr.checkInterruptAddr, RedBlueAddr.animateNidorinoAddr, RedBlueAddr.checkInterruptAddr, RedBlueAddr.joypadAddr },
        new Joypad[] { NO_INPUT, NO_INPUT, NO_INPUT, NO_INPUT, NO_INPUT, NO_INPUT, NO_INPUT, NO_INPUT, A | B | START | SELECT },
        new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 });

    private static readonly ResetStrat hop5Reset =
        new ResetStrat("_hop5(reset)", 363 + 487 + 3,
        new int[] { RedBlueAddr.animateNidorinoAddr, RedBlueAddr.checkInterruptAddr, RedBlueAddr.animateNidorinoAddr, RedBlueAddr.checkInterruptAddr, RedBlueAddr.animateNidorinoAddr, RedBlueAddr.checkInterruptAddr, RedBlueAddr.animateNidorinoAddr, RedBlueAddr.checkInterruptAddr, RedBlueAddr.animateNidorinoAddr, RedBlueAddr.checkInterruptAddr, RedBlueAddr.joypadAddr },
        new Joypad[] { NO_INPUT, NO_INPUT, NO_INPUT, NO_INPUT, NO_INPUT, NO_INPUT, NO_INPUT, NO_INPUT, NO_INPUT, NO_INPUT, A | B | START | SELECT },
        new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });

    private static readonly List<ResetStrat> hopResets = new List<ResetStrat>(new[] { hop1Reset, hop2Reset, hop3Reset, hop4Reset, hop5Reset });

    private class IntroSequence : List<Strat>, IComparable<IntroSequence>
    {
        public IntroSequence(params Strat[] strats)
            : base(strats)
        {
        }

        public IntroSequence(IntroSequence seq)
            : base(seq)
        {
        }

        public override string ToString()
        {
            var ret = gameName;
            foreach (var s in this)
            {
                ret += s.name;
            }
            return ret;
        }

        public void Execute(Rby gb)
        {
            foreach (var s in this)
            {
                s.Execute(gb);
            }
        }

        public int Cost()
        {
            var ret = 0;
            foreach (var s in this)
            {
                ret += s.cost;
            }

            return ret;
        }

        public int CompareTo(IntroSequence o)
        {
            return Cost() - o.Cost();
        }
    }

    private static List<IntroSequence> Permute(IEnumerable sl1, IEnumerable sl2)
    {
        List<IntroSequence> seqs = new List<IntroSequence>();
        foreach (Strat s1 in sl1)
        {
            foreach (Strat s2 in sl2)
            {
                IntroSequence seq = new IntroSequence
                {
                    s1,
                    s2
                };
                seqs.Add(seq);
            }
        }
        return seqs;
    }

    private static IntroSequence Append(IntroSequence seq, params Strat[] strats)
    {
        IntroSequence newSeq = new IntroSequence(seq);
        newSeq.AddRange(strats);
        return newSeq;
    }

    private static IntroSequence Append(IntroSequence seq1, IntroSequence seq2)
    {
        IntroSequence newSeq = new IntroSequence(seq1);
        newSeq.AddRange(seq2);
        return newSeq;
    }

    private static IntroSequence Append(Strat strat, IntroSequence seq)
    {
        IntroSequence newSeq = new IntroSequence(strat);
        newSeq.AddRange(seq);
        return newSeq;
    }

    private static int ReadTID(Rby gb)
    {
        return gb.CpuReadBE<ushort>("wPlayerID");
    }

    public static void DoSearch()
    {
        using var writer = new StreamWriter(gameName + "_tids.txt");

        var title = new List<Strat>();
        var titleResets = new List<Strat>();
        var titleusb = new List<Strat>();

        const int maxTitle = (MAX_COST - (492 + 172 + CRY_BASE_COST + TITLE_BASE_COST + 20 + 20));
        for (int i = 0; maxTitle >= 0 && i <= maxTitle / 270; i++)
        {
            int[] addr = new int[i + 1]; Joypad[] input = new Joypad[i + 1]; int[] advFrames = new int[i + 1];
            int[] rsAddr = new int[i + 1]; Joypad[] rsInput = new Joypad[i + 1]; int[] rsAdvFrames = new int[i + 1];
            int[] usbAddr = new int[i + 1]; Joypad[] usbInput = new Joypad[i + 1]; int[] usbAdvFrames = new int[i + 1];

            for (int j = 0; j < i; j++)
            {
                addr[j] = RedBlueAddr.titleScreenPickNewMonAddr;
                input[j] = NO_INPUT;
                advFrames[j] = 1;

                rsAddr[j] = RedBlueAddr.titleScreenPickNewMonAddr;
                rsInput[j] = NO_INPUT;
                rsAdvFrames[j] = 1;

                usbAddr[j] = RedBlueAddr.titleScreenPickNewMonAddr;
                usbInput[j] = NO_INPUT;
                usbAdvFrames[j] = 1;
            }

            addr[i] = RedBlueAddr.joypadAddr;
            input[i] = START;
            advFrames[i] = 1;
            title.Add(new Strat("_title" + i, CRY_BASE_COST + 270 * i, addr, input, advFrames));

            rsAddr[i] = RedBlueAddr.joypadAddr;
            rsInput[i] = A | B | START | SELECT;
            rsAdvFrames[i] = 1;
            titleResets.Add(new ResetStrat("_title" + i + "(reset)", 363 + 270 * i, rsAddr, rsInput, rsAdvFrames));

            usbAddr[i] = RedBlueAddr.joypadAddr;
            usbInput[i] = UP | SELECT | B;
            usbAdvFrames[i] = 1;
            titleusb.Add(new ResetStrat("_title" + i + "(usb)_csreset", CRY_BASE_COST + 363 + 270 * i, usbAddr, usbInput, usbAdvFrames));
        }

        var newGameSequences = new List<IntroSequence>();

        var resetSequences = new List<IntroSequence>
        {
            new IntroSequence(gfReset),
            new IntroSequence(gfWait, hop0Reset)
        };
        resetSequences.AddRange(Permute(gf, hopResets));

        var s3seqs = new List<IntroSequence>();
        s3seqs.AddRange(Permute(gf, nido));

        while (s3seqs.Count > 0)
        {
            var s4seqs = new List<IntroSequence>();
            foreach (IntroSequence s3 in s3seqs)
            {
                int ngcost = s3.Cost() + CRY_BASE_COST + 20 + 20;
                int ngmax = (MAX_COST - ngcost - 492);
                for (int i = 0; ngmax >= 0 && i <= ngmax / 270; i++)
                {
                    s4seqs.Add(Append(s3, title[i]));
                }

                int rscost = ngcost + 363 + 172 + TITLE_BASE_COST;
                int rsmax = (MAX_COST - rscost - 492);
                for (int j = 0; rsmax >= 0 && j <= rsmax / 270; j++)
                {
                    resetSequences.Add(Append(s3, titleResets[j]));
                    if (270 * j <= MAX_COST - 492 - CRY_BASE_COST)
                    {
                        resetSequences.Add(Append(s3, titleusb[j]));
                    }
                    // TODO: move this case to next loop now that there are options strats
                    if (270 * j <= MAX_COST - 492 - CRY_BASE_COST - 20)
                    {
                        resetSequences.Add(Append(s3, title[j], ngReset));
                    }
                }
            }

            s3seqs.Clear();

            while (s4seqs.Count > 0)
            {
                var s4tmp = new List<IntroSequence>();
                foreach (IntroSequence s4 in s4seqs)
                {
                    IntroSequence seq = Append(s4, newGame);
                    newGameSequences.Add(seq);

                    int ngcost = s4.Cost() + 20 + 20;
                    if ((MAX_COST - 492 - ngcost) >= 142 + TITLE_BASE_COST + CRY_BASE_COST)
                    {
                        s3seqs.Add(Append(s4, backout));
                    }

                    if (optsMode && (MAX_COST - 492 - ngcost) >= 72)
                    {
                        s4tmp.Add(Append(s4, options, optback));
                    }

                    int rscost = s4.Cost() + 20 + 20 + 114;
                    if ((MAX_COST - 492 - rscost) >= 363 + 172 + TITLE_BASE_COST + CRY_BASE_COST + 20 + 20)
                    {
                        resetSequences.Add(Append(s4, newGame, oakReset));
                    }

                    int rscost2 = s4.Cost() + 24;
                    if (optsMode && (MAX_COST - 492 - rscost2) >= 363 + 172 + TITLE_BASE_COST + CRY_BASE_COST + 20 + 20)
                    {
                        resetSequences.Add(Append(s4, optReset));
                    }

                    int rscost3 = s4.Cost() + 24 + 48;
                    if (optsMode && (MAX_COST - 492 - rscost3) >= 363 + 172 + TITLE_BASE_COST + CRY_BASE_COST + 20 + 20)
                    {
                        resetSequences.Add(Append(s4, options, optback, ngReset));
                    }
                }

                s4seqs = new List<IntroSequence>(s4tmp);
            }
        }

        resetSequences.Sort((x, y) => x.CompareTo(y));

        var introSequences = new List<IntroSequence>(newGameSequences);
        while (newGameSequences.Count > 0)
        {
            newGameSequences.Sort((x, y) => x.CompareTo(y));
            for (int i = 0; i < newGameSequences.Count; i++)
            {
                var ng = newGameSequences[i];
                newGameSequences.Remove(ng);

                foreach (IntroSequence rs in resetSequences)
                {
                    if (rs.Cost() + ng.Cost() <= MAX_COST - 492)
                    {
                        var newSeq = Append(rs, ng);
                        introSequences.Add(newSeq);
                        newGameSequences.Add(newSeq);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        Console.WriteLine("Number of intro sequences: " + introSequences.Count());
        introSequences.Sort((x, y) => x.CompareTo(y));

        Rby gb = gameName switch
        {
            "red" => new Red(),
            "blue" => new Blue(),
            _ => throw new InvalidOperationException(),
        };

        var beginState = gb.SaveState();
        //gb.Record("test");
        foreach (var seq in introSequences)
        {
            seq.Execute(gb);
            int tid = ReadTID(gb);
            writer.WriteLine(
                    seq.ToString()
                            + ": TID = " + $"0x{tid:X4}" + " (" + $"{tid:D5}"
                            + ", Cost: " + (seq.Cost() + 492) + ")");

            gb.LoadState(beginState);
            //writer.Flush();
            //Console.WriteLine("Current Cost: %d%n", seq.Cost());
        }
        writer.Close();
        gb.Dispose();
    }
}