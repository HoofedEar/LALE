﻿namespace LALE;

// HUMAN READABLE NAMES
public static class Names
{
    public static readonly string[] Sprites =
    {
        "00: ",
        "01: ",
        "02: ",
        "03: ",
        "04: ",
        "05: ",
        "06: ",
        "07: ",
        "08: ",
        "09: Red Warrior/Octorok",
        "0A: ",
        "0B: Hooded Stalfos/Blue Warrior",
        "0C: ",
        "0D: tektike (Hoppy crab thing)",
        "0E: leever (things that come out of the ground)",
        "0F: armos",
        "10: gravestone ghost?",
        "11: giant ghost?",
        "12: ghost?",
        "13: invalid opcode on overworld?",
        "14: sword knight",
        "15: anti-fairy",
        "16: Spark",
        "17: Spark",
        "18: Pols Voice",
        "19: Keese",
        "1a: blue stalfos",
        "1B: Zol",
        "1c: little red slime",
        "1d: nothing (?)",
        "1e: orange stalfos",
        "1f: mummy",
        "20: hardhat beetle",
        "21: wizzrobe",
        "22: ",
        "23: likelike (shield eater)",
        "24: shield face dudes",
        "25: ",
        "26: ",
        "27: Blade Trap",
        "28: ",
        "29: Mini: Moldorm",
        "2a: beamos (eye turret)",
        "2b: ",
        "2c: flippy shield enemy",
        "2D: Heart",
        "2E: Rupee",
        "2F: Fairy",
        "30: key",
        "31: sword pickup",
        "32: mummy you can walk over??",
        "33: piece of power",
        "34: acorn",
        "35: heart piece",
        "36: heart container",
        "37: blank?",
        "38: bomb pickup",
        "39: Instrument",
        "3A: toadstool",
        "3B: nothing?",
        "3C: ",
        "3D: ",
        "3E: ",
        "3F: Talon/Raccoon",
        "40: ",
        "41: owl actual bird",
        "42: Owl Statue",
        "43: ",
        "44: readable? dunno",
        "45: ",
        "46: ",
        "47: ",
        "48: ",
        "49: ",
        "4A: ",
        "4B: ",
        "4C: ",
        "4D: ",
        "4E: ",
        "4F: ",
        "50: Boo",
        "51: mace knight",
        "52: Ground Vacuum (pull)",
        "53: ground vacuum (push)",
        "54: fisherman",
        "55: bomb dude",
        "56: bomb dude that follows you when u hit it",
        "57: teleporting ninja penguin",
        "58: ",
        "59: Moldorm",
        "5A: Facade",
        "5B: Slime Eye",
        "5C: Genie",
        "5D: Slime Eel",
        "5E: ",
        "5F: ",
        "60: ",
        "61: Miniboss Portal / overworld warp",
        "62: Hothead",
        "63: Evil Eagle",
        "64: boss?",
        "65: Angler Fish",
        "66: raised Block Switch toggle",
        "67: ",
        "68: ",
        "69: ",
        "6a: raft",
        "6b: ",
        "6c: chicken",
        "6d: bowow",
        "6e: fly? maybe",
        "6f: town dog/fox/cat/whatever",
        "70: ",
        "71: ",
        "72: ",
        "73: ",
        "74: ",
        "75: ",
        "76: ",
        "77: ",
        "78: ",
        "79: ",
        "7A: crow",
        "7B: ",
        "7C: ",
        "7D: ",
        "7E: ",
        "7F: ",
        "80: ",
        "81: Rolling Bones",
        "82: Miniboss Object",
        "83: ",
        "84: fairy from shrine",
        "85: ",
        "86: Floating Magic/Bombs",
        "87: desert lanmola (vortex thingy)",
        "88: ",
        "89: Orange Hinox (bomb cyke)",
        "8A: ",
        "8B: ",
        "8C: ",
        "8D: ",
        "8E: cue ball (angry water miniboss)",
        "8F: Arm Mimic",
        "90: Three of a Kind",
        "91: fake kirby",
        "92: ",
        "93: bomber knight",
        "94: ",
        "95: ",
        "96: ",
        "97: ",
        "98: ",
        "99: water tektite (slidy crab)",
        "9A: ",
        "9B: hidden slime",
        "9C: ",
        "9d: heavy face shrine power bracelet thing",
        "9e: turret",
        "9f: goomba (side view)",
        "A0: ",
        "A1: ",
        "A2: ",
        "A3: ",
        "A4: ",
        "A5: ",
        "A6: ",
        "A7: ",
        "A8: ",
        "A9: ",
        "AA: ",
        "AB: ",
        "AC: ",
        "AD: ",
        "AE: ",
        "AF: ",
        "B0: ",
        "B1: ",
        "B2: ",
        "B3: ",
        "B4: ",
        "B5: ",
        "B6: ",
        "B7: ",
        "B8: ",
        "b9: buzz blob",
        "BA: mushroom bomber flying guy",
        "BB: ",
        "BC: ",
        "BD: ",
        "BE: ",
        "bf: zombie spawner",
        "C0: ",
        "C1: ",
        "C2: ",
        "C3: ",
        "C4: ",
        "c5: spiny",
        "C6: ",
        "C7: ",
        "C8: ",
        "C9: ",
        "CA: ",
        "cb: zora",
        "CC: ",
        "CD: ",
        "CE: ",
        "CF: ",
        "D0: ",
        "D1: ",
        "D2: ",
        "D3: ",
        "D4: ",
        "D5: ",
        "D6: ",
        "D7: ",
        "D8: ",
        "D9: ",
        "DA: ",
        "DB: ",
        "DC: ",
        "DD: ",
        "DE: ",
        "DF: ",
        "E0: ",
        "E1: ",
        "E2: wall flamethrower",
        "E3: ",
        "E4: ",
        "E5: Floating Heart/Arrows",
        "E6: ",
        "E7: ",
        "E8: ",
        "e9: red shell guy",
        "ea: green shell guy",
        "EB: ",
        "ec: red camo goblin",
        "ed: green camo goblin",
        "EE: ",
        "ef: color dungeon spinner switch",
        "F0: ",
        "F1: ",
        "f2: bone putter",
        "f3: bone hopper?",
        "F4: ",
        "F5: ",
        "F6: ",
        "F7: ",
        "F8: ",
        "F9: ",
        "fa: ?? used in a pit room"

        // AUTOFILL REMAINDER
    }; // sprites

    public static string GetName(string[] list, int index)
    {
        if (index < list.Length && index >= 0)
        {
            return list[index];
        }

        return index.ToString("X2") + ": ??";
    } //getname

} // class
// ns
