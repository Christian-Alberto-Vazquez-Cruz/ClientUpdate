using System;
using System.ServiceModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using TripasDeGatoCliente.TripasDeGatoServicio;
using TripasDeGatoCliente.Logic;
using System.Threading.Tasks;
using System.Timers;

namespace TripasDeGatoCliente.Views
{
    public partial class LobbyView : Page, IChatManagerCallback, ILobbyManagerCallback
    {
        private ChatManagerClient chatManager;
        private LobbyManagerClient lobbyManager;
        private LobbyBrowserClient lobbyBrowser;
        private Timer refreshTimer;
        public LobbyView(string lobbyCode)
        {
            InitializeComponent();
            lbCode.Content = lobbyCode;
            labelPlayer1.Content = UserProfileSingleton.Nombre;
            lobbyBrowser = new LobbyBrowserClient();
            //ESTO HACÍA ANTES
            //lobbyManager = new LobbyManagerClient(new InstanceContext(this));
            chatManager = new ChatManagerClient(new InstanceContext(this));
            StartRefreshTimer();
            //InitializeChatAsync();
        }

        /*private async void InitializeChatAsync()
        {
            try
            {
                await Task.Run(() => chatManager.connectToLobby(UserProfileSingleton.Nombre));
                SendWelcomeNotification();
            }
            catch (Exception ex)
            {
                HandleChatException(ex);
            }
        }*/

        private void StartRefreshTimer() {
            refreshTimer = new Timer(2000); // Set timer to trigger every 2 second
            refreshTimer.Elapsed += async (sender, e) => await RefreshLobbyData();
            refreshTimer.Start();
        }

        //SIN EL IF(APPLICATION.CURRENT.DISPATCHER.CHECKACCESS())
        /*private async Task RefreshLobbyData() {
            try {
                // Fetch the latest lobby data
                var lobby = await lobbyBrowser.GetLobbyByCodeAsync(lbCode.Content.ToString());
                Dispatcher.Invoke(() =>
                {
                    labelPlayer2.Text = lobby.Players.ContainsKey("PlayerTwo") ? lobby.Players["PlayerTwo"].userName: "Esperando jugador...";
                    // Additional UI updates can be added here
                });
            } catch (Exception ex) {
                Dispatcher.Invoke(() => MessageBox.Show($"Error al actualizar datos del lobby: {ex.Message}"));
            }
        }*/

        private async Task RefreshLobbyData() {
            try {
                //Versión anterior
                //var lobby = await lobbyBrowser.GetLobbyByCodeAsync(lbCode.Content.ToString());
                string lobbyCode = lbCode.Content.ToString();
                Lobby lobby = await Task.Run(() => lobbyBrowser.GetLobbyByCode(lobbyCode));
                if (Application.Current.Dispatcher.CheckAccess()) {
                    labelPlayer2.Text = lobby.Players.ContainsKey("PlayerTwo") ? lobby.Players["PlayerTwo"].userName : "Esperando jugador...";
                    DialogManager.ShowWarningMessageAlert("Check Access true");
                } else {
                    Dispatcher.Invoke(() => {
                        labelPlayer2.Text = lobby.Players.ContainsKey("PlayerTwo") ? lobby.Players["PlayerTwo"].userName : "Esperando jugador...";
                        // Additional UI updates can be added here
                    });
                }
            } catch (Exception ex) {
                Dispatcher.Invoke(() => MessageBox.Show($"Error al actualizar datos del lobby: {ex.Message}"));
            }
        }

        private async void SendWelcomeNotification()
        {
            var message = new Message
            {
                chatMessage = $"{UserProfileSingleton.Nombre} se ha unido al chat.",
                userName = "Server notification"
            };

            try
            {
                await Task.Run(() => chatManager.sendMessage(UserProfileSingleton.Nombre, message)); 
            }
            catch (Exception ex)
            {
                HandleChatException(ex);
            }
        }

        public void broadcastMessage(Message message)
        {
            Dispatcher.Invoke(() =>
            {
                Border messageContainer = new Border
                {
                    Background = new SolidColorBrush(Colors.White),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(10),
                    Margin = new Thickness(20, 5, 20, 5),
                    HorizontalAlignment = message.userName == UserProfileSingleton.Nombre ? HorizontalAlignment.Right : HorizontalAlignment.Left
                };

                TextBlock messageBlock = new TextBlock
                {
                    Text = $"{message.chatMessage} {DateTime.Now:HH:mm}",
                    Foreground = new SolidColorBrush(Colors.Black),
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 250
                };

                messageContainer.Child = messageBlock;
                ChatMessagesPanel.Children.Add(messageContainer);
                ScrollToBottom();
            });
        }

        private async void BtnSendMessage_Click(object sender, RoutedEventArgs e)
        {
            string messageText = txtMessageInput.Text.Trim();

            if (!string.IsNullOrEmpty(messageText))
            {
                var message = new Message
                {
                    userName = UserProfileSingleton.Nombre,
                    chatMessage = messageText
                };

                try
                {
                    await Task.Run(() => chatManager.sendMessage(UserProfileSingleton.Nombre, message));
                    txtMessageInput.Clear();
                }
                catch (Exception ex)
                {
                    HandleChatException(ex);
                }
            }
        }

        private void HandleChatException(Exception ex)
        {
            MessageBox.Show($"Error en la comunicación del chat: {ex.Message}");
        }

        private void ScrollToBottom()
        {
            var scrollViewer = VisualTreeHelper.GetParent(ChatMessagesPanel) as ScrollViewer;
            scrollViewer?.ScrollToEnd();
        }



        private void GoToMenuView()
        {
            MenuView menuView = new MenuView();
            if (this.NavigationService != null)
            {
                this.NavigationService.Navigate(menuView);
            }
            else
            {
                MessageBox.Show("Error: No se puede navegar al menu.");
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            GoToMenuView();
        }

        public void notifyPlayerConectedCallback(Profile guest) {
            Dispatcher.Invoke(() => {
                labelPlayer2.Text = guest.userName;
            });
        }

        public void removeFromLobby() {
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show("Has sido eliminado del lobby.");
                GoToMenuView();
            });
        }

        public void hostLeftCallback() {
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show("El anfitrión ha abandonado el lobby. Serás redirigido al menú.");
                GoToMenuView();
            });
        }

        public void guestLeftCallback() {
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show("El invitado ha abandonado el lobby.");
                labelPlayer2.Text = "Esperando jugador...";
            });
        }
    }
}
