using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TripasDeGatoCliente.Logic;
using TripasDeGatoCliente.TripasDeGatoServicio;

namespace TripasDeGatoCliente.Views {
    public partial class SelectLobbyView : Page {
        private LobbyBrowserClient lobbyBrowser;
        private LobbyManagerClient lobbyManager;
        

        public SelectLobbyView() {
            InitializeComponent();
            //ESTO HACÍA ANTES, NO FUNCIONA
            //InstanceContext context = new InstanceContext(this);
            //lobbyManager = new LobbyManagerClient(context);
            lobbyBrowser = new LobbyBrowserClient();

            LoadLobbiesAsync();
        }

        private Task LoadLobbiesAsync() {
            try {
                // Llamada al servicio para obtener los lobbies disponibles
                var lobbies = lobbyBrowser.GetAvailableLobbies();
                LobbyDataGrid.ItemsSource = lobbies;
            } catch (Exception ex) {
                MessageBox.Show($"Error al cargar los lobbies: {ex.Message}");
            }

            return Task.CompletedTask;
        }

        private async void btnJoinGame_Click(object sender, RoutedEventArgs e) {
            // Verificar si se ha seleccionado un lobby en el DataGrid
            if (LobbyDataGrid.SelectedItem is Lobby selectedLobby) {
                string lobbyCode = selectedLobby.Code;
                Profile guest = new Profile {
                    idProfile = UserProfileSingleton.IdPerfil,
                    userName = UserProfileSingleton.Nombre
                };

                try {
                    // Intentar unirse al lobby seleccionado como JugadorDos
                    bool joined = await lobbyBrowser.JoinLobbyAsync(lobbyCode, guest);

                    if (joined) {
                        LobbyView lobbyView = new LobbyView(lobbyCode);
                        this.NavigationService.Navigate(lobbyView);
                    } else {
                        MessageBox.Show("No se pudo unir al lobby. Puede que esté lleno o que ya no esté disponible.");
                    }
                } catch (Exception ex) {
                    MessageBox.Show($"Error al intentar unirse al lobby: {ex.Message}");
                }
            } else {
                MessageBox.Show("Por favor, selecciona un lobby para unirte.");
            }
        }


        private void btnBack_Click(object sender, RoutedEventArgs e) {
            MenuView menuView = new MenuView();
            if (this.NavigationService != null) {
                this.NavigationService.Navigate(menuView);
            } else {
                MessageBox.Show("Error: No se puede navegar al menu.");
            }
        }
    }
}
