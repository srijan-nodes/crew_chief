using System;

namespace CrewChiefV4
{
    [Flags]
    public enum GameEnum : ulong
    {
        ACC                 = 1 << 0,
        AMS2                = 1 << 1,
        AMS2_NETWORK        = 1 << 2,
        ASSETTO_32BIT       = 1 << 3,
        ASSETTO_64BIT       = 1 << 4,
        ASSETTO_128CARS     = 1 << 5,
        ASSETTO_64BIT_RALLY = 1 << 6,
        ASSETTO_EVO         = 1 << 7,
        ASSETTO_PRO         = 1 << 8,
        DIRT                = 1 << 9,
        DIRT_2              = 1 << 10,
        F1_2018             = 1 << 11,
        F1_2019             = 1 << 12,
        F1_2020             = 1 << 13,
        F1_2021             = 1 << 14,
        F1_2022             = 1 << 15,
        F1_2023             = 1 << 16,
        GTR2                = 1 << 17,
        IRACING             = 1 << 18,
        LMU                 = 1 << 19,
        PCARS2              = 1 << 20,
        PCARS2_NETWORK      = 1 << 21,
        PCARS3              = 1 << 22,
        PCARS_32BIT         = 1 << 23,
        PCARS_64BIT         = 1 << 24,
        PCARS_NETWORK       = 1 << 25,
        RACE_ROOM           = 1 << 26,
        RBR                 = 1 << 27,
        RF1                 = 1 << 28,
        RF2_64BIT           = 1 << 29,
        PMR                 = 1 << 30,

        // the bitshift operator always only can bitshift int values so do these special values manually
        UNKNOWN             = 0b00100000_00000000_00000000_00000000_00000000_00000000_00000000_00000000,
        NONE                = 0b01000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000,
                                /* NONE allows CC macros to run when an unsupported game is being played, it's selectable from the Games list */
        ANY                 = 0b10000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000
                                /* ANY allows CC macros to be defined that apply to all supported games, it's only selectable from the macro UI */
}

    public static class Game
    {
        public static GameEnum game { get; set; }

        public static bool ACC => game == GameEnum.ACC;
        public static bool AMS2 => game == GameEnum.AMS2;
        public static bool AMS2_NETWORK => game == GameEnum.AMS2_NETWORK;
        public static bool ASSETTO_32BIT => game == GameEnum.ASSETTO_32BIT;
        public static bool ASSETTO_64BIT => game == GameEnum.ASSETTO_64BIT;
        public static bool ASSETTO_64BIT_RALLY => game == GameEnum.ASSETTO_64BIT_RALLY;
        public static bool ASSETTO_EVO => game == GameEnum.ASSETTO_EVO;
        public static bool ASSETTO_PRO => game == GameEnum.ASSETTO_PRO;
        public static bool DIRT => game == GameEnum.DIRT;
        public static bool DIRT_2 => game == GameEnum.DIRT_2;
        public static bool F1_2018 => game == GameEnum.F1_2018;
        public static bool F1_2019 => game == GameEnum.F1_2019;
        public static bool F1_2020 => game == GameEnum.F1_2020;
        public static bool F1_2021 => game == GameEnum.F1_2021;
        public static bool F1_2022 => game == GameEnum.F1_2022;
        public static bool F1_2023 => game == GameEnum.F1_2023;
        public static bool GTR2 => game == GameEnum.GTR2;
        public static bool IRACING => game == GameEnum.IRACING;
        public static bool LMU => game == GameEnum.LMU;
        public static bool PCARS2 => game == GameEnum.PCARS2;
        public static bool PCARS2_NETWORK => game == GameEnum.PCARS2_NETWORK;
        public static bool PCARS3 => game == GameEnum.PCARS3;
        public static bool PCARS_32BIT => game == GameEnum.PCARS_32BIT;
        public static bool PCARS_64BIT => game == GameEnum.PCARS_64BIT;
        public static bool PCARS_NETWORK => game == GameEnum.PCARS_NETWORK;
        public static bool PMR => game == GameEnum.PMR;
        public static bool RACE_ROOM => game == GameEnum.RACE_ROOM;
        public static bool RBR => game == GameEnum.RBR;
        public static bool RF1 => game == GameEnum.RF1;
        public static bool RF2_64BIT => game == GameEnum.RF2_64BIT;
        public static bool UNKNOWN => game == GameEnum.UNKNOWN;
        public static bool NONE => game == GameEnum.NONE;
        public static bool ANY => game == GameEnum.ANY;

        public static bool ASSETTO1 => (game & (GameEnum.ASSETTO_32BIT | GameEnum.ASSETTO_64BIT | GameEnum.ASSETTO_128CARS | GameEnum.ASSETTO_PRO)) != 0;
        public static bool PCARS1 => (game & (GameEnum.PCARS_32BIT | GameEnum.PCARS_64BIT | GameEnum.PCARS_NETWORK)) != 0;
        public static bool PCARS_2_3 => (game & (GameEnum.PCARS2 | GameEnum.PCARS3 | GameEnum.PCARS2_NETWORK)) != 0;
        public static bool PCARS_AMS2 => PCARS1 || PCARS_2_3 || Game.AMS2  || Game.AMS2_NETWORK;
        public static bool RF2_LMU => (game & (GameEnum.RF2_64BIT | GameEnum.LMU)) != 0;
        public static bool F1_20S => (game & (GameEnum.F1_2018 | GameEnum.F1_2019 | GameEnum.F1_2020 | GameEnum.F1_2021 | GameEnum.F1_2022 | GameEnum.F1_2023)) != 0;
    }

    /// <summary>
    /// Workarounds for games that have bugs or missing features.
    /// These MAY be a temporary solution until the game developers fix the issues.
    /// </summary>
    public static class GameWorkArounds
    {
        public static bool LMU_FuelMultiplier  => Game.LMU;   // LMU doesn't report FuelMultiplier
        public static bool LMU_PitstopGoGoGo   => Game.LMU;   // LMU sets IsPitCrewDone as soon as car stops.
    }
}
