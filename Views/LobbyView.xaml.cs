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

namespace TripasDeGatoCliente.Views {
    public partial class LobbyView : Page, IChatManagerCallback, ILobbyManagerCallback {
        private ChatManagerClient chatManager;
        private LobbyManagerClient lobbyManager;
        private LobbyBrowserClient lobbyBrowser;
        private Timer refreshTimer;
        private string lobbyCode; // Variable global para almacenar el código del lobby

        public LobbyView(string lobbyCode) {
            InitializeComponent();
            this.lobbyCode = lobbyCode; // Asignación inicial del código
            lbCode.Content = lobbyCode; // Visualización en el label
            lobbyBrowser = new LobbyBrowserClient();
            InitializeLobby();
            lobbyManager = new LobbyManagerClient(new InstanceContext(this));
            chatManager = new ChatManagerClient(new InstanceContext(this));
            StartRefreshTimer();
            InitializeChatAsync();
        }

        private async void InitializeChatAsync() {
            try {
                await Task.Run(() => chatManager.ConnectToChat(UserProfileSingleton.Nombre));
                //SendWelcomeNotification();
            } catch (Exception ex) {
                HandleChatException(ex);
            }
        }

        public async void InitializeLobby() {
            Lobby lobby = await lobbyBrowser.GetLobbyByCodeAsync(lobbyCode);

            labelPlayer2.Text = lobby.Players.ContainsKey("PlayerTwo") ? lobby.Players["PlayerTwo"].userName : "Esperando jugador...";
            labelPlayer1.Content = lobby.Players.ContainsKey("PlayerOne") ? lobby.Players["PlayerOne"].userName : "Esperando jugador...";
        }

        private void StartRefreshTimer() {
            refreshTimer = new Timer(1000);
            refreshTimer.Elapsed += async (sender, e) => await RefreshLobbyData();
            refreshTimer.Start();
        }

        private async Task RefreshLobbyData() {
            try {
                Dispatcher.Invoke(() => {
                    Lobby lobby = lobbyBrowser.GetLobbyByCode(lobbyCode);
                    if (lobby != null) {
                        labelPlayer2.Text = lobby.Players.ContainsKey("PlayerTwo") ? lobby.Players["PlayerTwo"].userName : "Esperando jugador...";
                    }
                });
            } catch (InvalidOperationException invalidOperationException) {
                Console.WriteLine($"Se intentó modificar la UI desde un hilo incorrecto, {invalidOperationException.Message}");
            } catch (Exception ex) {
                Console.WriteLine($"Ocurrió un error inesperado, {ex}");
            }
        }

        private async void SendWelcomeNotification() {
            var message = new Message {
                chatMessage = $"{UserProfileSingleton.Nombre} se ha unido al chat.",
                userName = "Server notification"
            };

            try {
                await Task.Run(() => chatManager.SendMessage(UserProfileSingleton.Nombre, message));
            } catch (Exception ex) {
                HandleChatException(ex);
            }
        }

        private async void BtnSendMessage_Click(object sender, RoutedEventArgs e) {
            string messageText = txtMessageInput.Text.Trim();

            if (!string.IsNullOrEmpty(messageText)) {
                var message = new Message {
                    userName = UserProfileSingleton.Nombre,
                    chatMessage = messageText
                };

                try {
                    await Task.Run(() => chatManager.SendMessageAsync(UserProfileSingleton.Nombre, message));
                    txtMessageInput.Clear();
                } catch (Exception ex) {
                    HandleChatException(ex);
                }
            }
        }

        private void HandleChatException(Exception ex) {
            MessageBox.Show($"Error en la comunicación del chat: {ex.Message}");
        }

        private void ScrollToBottom() {
            var scrollViewer = VisualTreeHelper.GetParent(ChatMessagesPanel) as ScrollViewer;
            scrollViewer?.ScrollToEnd();
        }

        private void GoToMenuView() {
            MenuView menuView = new MenuView();
            if (this.NavigationService != null) {
                this.NavigationService.Navigate(menuView);
            } else {
                MessageBox.Show("Error: No se puede navegar al menu.");
            }
        }

        private async void BtnBack_Click(object sender, RoutedEventArgs e) {
            try {
                await lobbyManager.LeaveLobbyAsync(lobbyCode, UserProfileSingleton.IdPerfil);
                GoToMenuView();
            } catch (Exception ex) {
                MessageBox.Show($"Error al salir del lobby: {ex.Message}");
            }
        }

        public void RemoveFromLobby() {
            Dispatcher.Invoke(() => {
                MessageBox.Show("Has sido eliminado del lobby.");
                GoToMenuView();
            });
        }

        public void HostLeftCallback() {
            Dispatcher.Invoke(() => {
                MessageBox.Show("El anfitrión ha abandonado el lobby. Serás redirigido al menú.");
                GoToMenuView();
            });
        }

        public void GuestLeftCallback() {
            Dispatcher.Invoke(() => {
                MessageBox.Show("El invitado ha abandonado el lobby.");
                labelPlayer2.Text = "Esperando jugador...";
            });
        }

        public void BroadcastMessage(Message message) {
            Application.Current.Dispatcher.Invoke(() => {
                Border messageContainer = new Border {
                    Background = new SolidColorBrush(Colors.White),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(10),
                    Margin = new Thickness(20, 5, 20, 5),
                    HorizontalAlignment = message.userName == UserProfileSingleton.Nombre ? HorizontalAlignment.Right : HorizontalAlignment.Left
                };

                TextBlock messageBlock = new TextBlock {
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
    }
}
