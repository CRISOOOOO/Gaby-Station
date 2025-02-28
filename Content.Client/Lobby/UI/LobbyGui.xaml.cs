using Content.Client.Message;
using Content.Client.UserInterface.Systems.EscapeMenu;
using Robust.Client.AutoGenerated;
using Robust.Client.Console;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.XAML;
using Robust.Client.ResourceManagement;

namespace Content.Client.Lobby.UI
{
    [GenerateTypedNameReferences]
    public sealed partial class LobbyGui : UIScreen
    {
        [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
        [Dependency] private readonly IResourceCache _resCache = default!;

        public LobbyGui()
        {
            RobustXamlLoader.Load(this);
            IoCManager.InjectDependencies(this);
            SetAnchorPreset(MainContainer, LayoutPreset.Wide);
            SetAnchorPreset(Background, LayoutPreset.Wide);

            var logoTexture = _resCache.GetResource<TextureResource>("/Textures/Logo/lobby.png");
            Logo.Texture = logoTexture;

            AHelpButton.AddStyleClass("ButtonColorGaby");
            BalanceButton.AddStyleClass("ButtonColorGaby");
            CallVoteButton.AddStyleClass("ButtonColorGaby");
            OptionsButton.AddStyleClass("ButtonColorGaby");
            LeaveButton.AddStyleClass("ButtonColorGaby");

            LobbySong.SetMarkup(Loc.GetString("lobby-state-song-no-song-text"));

            LeaveButton.OnPressed += _ => _consoleHost.ExecuteCommand("disconnect");
            BalanceButton.OnPressed += _ => _consoleHost.ExecuteCommand("balanceui");
            OptionsButton.OnPressed += _ => UserInterfaceManager.GetUIController<OptionsUIController>().ToggleWindow();
            ShowMOTD.OnPressed += _ =>
            {
                ShowMOTD.Visible = false;
                MOTDBuletin.Visible = true;
            };
            MOTDBuletin.ShowMOTDButton = ShowMOTD;
        }

        public void SwitchState(LobbyGuiState state)
        {
            DefaultState.Visible = false;
            CharacterSetupState.Visible = false;

            switch (state)
            {
                case LobbyGuiState.Default:
                    DefaultState.Visible = true;
                    RightSide.Visible = true;
                    break;
                case LobbyGuiState.CharacterSetup:
                    CharacterSetupState.Visible = true;

                    var actualWidth = (float) UserInterfaceManager.RootControl.PixelWidth;
                    var setupWidth = (float) LeftSide.PixelWidth;

                    if (1 - (setupWidth / actualWidth) > 0.30)
                    {
                        RightSide.Visible = false;
                    }

                    UserInterfaceManager.GetUIController<LobbyUIController>().ReloadCharacterSetup();

                    break;
            }
        }

        public enum LobbyGuiState : byte
        {
            /// <summary>
            ///  The default state, i.e., what's seen on launch.
            /// </summary>
            Default,
            /// <summary>
            ///  The character setup state.
            /// </summary>
            CharacterSetup
        }
    }
}
