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
                "~b~" + Settings.General.Name + ": ~w~Hi, is everything okay?",
                "~b~Person: ~w~Not really, who called you? My wife? She worries way too much when I go on my walks.", 
            },
            new string[]
            {
                "~b~" + Settings.General.Name + ": ~w~We received a call that you may be lost. Do you need my assistance?",
                "~b~Person: ~w~No, I'm good, who called you?", 
            },
            new string[]
            {
                "~b~" + Settings.General.Name + ": ~w~Hello, can we talk for a minute?",
                "~b~Person: ~w~Sure. What's up?",
                "~b~" + Settings.General.Name + ": ~w~We got a call from someone who was worried about your welfare.",
                "~b~Person: ~w~Huh. I'm just fine, Officer. I don't know why anyone thinks there's something wrong!",
            }
        };


        public static string[][] PersonIsInjured = new string[][]
        {
            new string[]
            {
                "~b~" + Settings.General.Name + ": ~w~We received a call that you may be lost. Do you need my assistance?",
                "~b~Person: ~w~Yes, please. I hurt my ankle really badly and I can't move.", 
                "~b~Person: ~w~Can you call a ambulance? I've been stuck here for hours.", 
            },
            new string[]
            {
                "~b~" + Settings.General.Name + ": ~w~We received a call that you may be lost. Do you need my assistance?",
                "~b~Person: ~w~My chest feels tight and I'm so short of breath I can hardly walk.", 
                "~b~Person: ~w~I don't think I can make it out of here on my own two feet.", 
            },
            new string[]
            {
                "~b~" + Settings.General.Name + ": ~w~We received a call that you may be lost. Do you need my assistance?",
                "~b~Person: ~w~My ankle really hurts. I can't move around at all!",
                "~b~Person: ~w~Please help me out!.",
            }
        };


        public static string[][] PersonIsDrunk = new string[][]
        {
            new string[]
            {
                "~b~" + Settings.General.Name + ": ~w~Do you need help?",
                "~b~Person: ~w~Who the fuck are you? I was here, chilling, having a few beers. I don't need any help.", 
            },
            new string[]
            {
                "~b~" + Settings.General.Name + ": ~w~We received a call that you may be lost. Do you need my assistance?",
                "~b~Person: ~w~Yes, I was with my friends, having, you know, a few whisky bottles and I fell asleep", 
                "~b~Person: ~w~When I woke up I was here and I don't know where I am.", 
            },
        };


        public static string[][] PersonIsScared = new string[][]
        {
            new string[]
            {
                "~b~" + Settings.General.Name + ": ~w~Do you need my assistance?",
                "~b~Person: ~w~Yes, I'm so scared. I have seen lots of aggressive animals around here.", 
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
                "~b~Person: ~w~I couldn't even see what the animal was. It all happened so fast!",
            },
        };
    }
}
