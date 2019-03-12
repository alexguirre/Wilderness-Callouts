namespace WildernessCallouts.Menus
{
    using Rage;
    using RAGENativeUI;
    using RAGENativeUI.Elements;
    using WildernessCallouts.Types;

    internal static class MenuCommon
    {
        public static MenuPool Pool;

        public static void InitializeAllMenus()
        {
            Pool = new MenuPool();

            InteractionMenu.Initialize();
            
            Logger.LogTrivial(typeof(MenuCommon).Name, "All menus initialized");
        }
    }
}
