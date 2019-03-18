using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PoliceSmartRadio;
using Rage;
using WildernessCallouts.CalloutFunct;
using WildernessCallouts.Peds;

namespace WildernessCallouts.Integrations
{
    /// <summary>
    /// The class that will handle integration with Police Smart Radio.
    /// </summary>
    class PoliceSmartRadioFunctions
    {
        /// <summary>
        /// Adds an Action and an availability check to the specified button. Only buttons contained in a folder matching your plugin's name can be manipulated.
        /// </summary>
        /// <param name="action">The action to execute if the button is selected.</param>
        /// <param name="buttonName">The texture file name of the button, excluding any directories or file extensions.</param>
        /// <returns></returns>
        public static bool AddActionToButton(Action action, string buttonName)
        {
            return PoliceSmartRadio.API.Functions.AddActionToButton(action, buttonName);
        }

        /// <summary>
        /// Adds an Action and an availability check to the specified button. Only buttons contained in a folder matching your plugin's name can be manipulated.
        /// </summary>
        /// <param name="action">The action to execute if the button is selected.</param>
        /// <param name="isAvailable">Function returning a bool indicating whether the button is currently available (if false, button is hidden). This is often called, so try making this light-weight (e.g. simply return the value of a boolean property). Make sure to do proper checking in your Action too, as the user can forcefully display all buttons via a setting in their config file.</param>
        /// <param name="buttonName">The texture file name of the button, excluding any directories or file extensions.</param>
        /// <returns></returns>
        public static bool AddActionToButton(Action action, Func<bool> isAvailable, string buttonName)
        {
            return PoliceSmartRadio.API.Functions.AddActionToButton(action, isAvailable, buttonName);
        }

        /// <summary>
        /// Set up the buttons.
        /// </summary>
        public PoliceSmartRadioFunctions()
        {
            try
            {
                AddActionToButton(CallVet, VetIsAvailable, "vet");
                AddActionToButton(CallAirParamedic, AirParamedicIsAvailable, "airparamedic");
            }
            catch (Exception e)
            {
                Logger.LogException(e);
            }
        }

        /// <summary>
        /// Try to call the vet.
        /// </summary>
        public void CallVet()
        {
            GameFiber.StartNew(() =>
            {
                try
                {
                    Ped animal = WildernessCallouts.Common.GetValidAnimalForVetPickup();

                    Vet vet = new Vet(animal);
                    vet.Start();
                }
                catch (Exception e)
                {
                    Logger.LogException(e);
                    Game.DisplayNotification("Unable to call the vet at this time. See RagePluginHook.log for details.");
                }
            }, "CallVet");

        }


        /// <summary>
        /// Try to call the air paramedic.
        /// </summary>
        public void CallAirParamedic()
        {
            GameFiber.StartNew(() =>
            {
                try
                {
                    Ped pedToRescue = WildernessCallouts.Common.GetPedToRescue();

                    AirParamedic airParamedic = new AirParamedic(pedToRescue);
                    airParamedic.Start();
                }
                catch (Exception e)
                {
                    Logger.LogException(e);
                    Game.DisplayNotification("Unable to call the vet at this time. See RagePluginHook.log for details.");
                }
            }, "CallAirParamedic");

        }


        /// <summary>
        /// Whether or not to display the vet radio button. Called frequently (whenever
        /// the user scrolls in the radio), so be sure to be conscious of performance issues.
        /// </summary>
        public bool VetIsAvailable()
        {
            try
            {
                return Common.GetClosestAnimal(30, 8);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Whether or not to display the air ambulance radio button. Called frequently (whenever
        /// the user scrolls in the radio), so be sure to be conscious of performance issues.
        /// </summary>
        /// <returns></returns>
        public bool AirParamedicIsAvailable()
        {
            try
            {
                Ped ped = Common.GetPedToRescue();
                Logger.LogDebug($"Air paramedic is avail: {ped}");
                return ped;
            }
            catch
            {
                return false;
            }
        }

    }
}
