using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WildernessCallouts.Dialogues
{
    internal class MissingPersonDialogues
    {
        public static string[][] PersonIsOk = new string[][]
        {
            new string[]
            {
                "~b~" + Settings.General.Name + ": ~w~Hi, do you have any problem?",
                "~b~Person: ~w~Not really, who call you? My wife? She worries too much when I go on my walks.", 
            },
            new string[]
            {
                "~b~" + Settings.General.Name + ": ~w~We received a call that you may be lost. Do you need my assistance?",
                "~b~Person: ~w~No, I'm good, I dont know who could have call you", 
            },
        };


        public static string[][] PersonIsInjured = new string[][]
        {
            new string[]
            {
                "~b~" + Settings.General.Name + ": ~w~We received a call that you may be lost. Do you need my assistance?",
                "~b~Person: ~w~Yes, please. I hurt my ankle and I can't move.", 
                "~b~Person: ~w~Can you call a ambulance I've been stuck here for hours.", 
            },
            new string[]
            {
                "~b~" + Settings.General.Name + ": ~w~We received a call that you may be lost. Do you need my assistance?",
                "~b~Person: ~w~My chest feels tight and I'm so short of breath I can hardly walk.", 
                "~b~Person: ~w~I don't think I can make it out of here on my own to feet.", 
            },
        };


        public static string[][] PersonIsDrunk = new string[][]
        {
            new string[]
            {
                "~b~" + Settings.General.Name + ": ~w~Do you need help?",
                "~b~Person: ~w~Who the fuck are you? I was here relaxed having few beers, I don't need any help.", 
            },
            new string[]
            {
                "~b~" + Settings.General.Name + ": ~w~We received a call that you may be lost. Do you need my assistance?",
                "~b~Person: ~w~Yes, I was with my friends having few whisky bottles and I fell asleep,", 
                "~b~Person: ~w~and then when I woke up I was here and I don't know where I am.", 
            },
        };


        public static string[][] PersonIsScared = new string[][]
        {
            new string[]
            {
                "~b~" + Settings.General.Name + ": ~w~Do you need my assistance?",
                "~b~Person: ~w~Yes, I'm so scared. I have seen lots of aggressive animals around here", 
                "~b~Person: ~w~Please help me!", 
            },
        };

        public static string[][] PersonIsVeryInjured = new string[][]
        {
            new string[]
            {
                "~b~Person: ~w~Thank goodness! Finally someone...", 
                "~b~Person: ~w~I'm very injured and I can't even move, I've been here for hours", 
            },

            new string[]
            {
                "~b~Person: ~w~Finally someone came here!", 
                "~b~Person: ~w~I was taking a walk and...",
                "~b~Person: ~w~Oh fuck! This hurts!",
                "~b~Person: ~w~...and an animal came from nowhere and started attacking me!",
                "~b~Person: ~w~I couldn't even see what animal was, all happened so fast!",
            },
        };
    }
}
