using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TripasDeGatoCliente.Logic;
using TripasDeGatoCliente.TripasDeGatoServicio;

namespace TripasDeGatoCliente.Views {
    public partial class SelectLobbyView : Page {
        private LobbyManagerClient lobbyManager;

        public SelectLobbyView() {
            InitializeComponent();
            lobbyManager = new LobbyManagerClient();
            LoadLobbiesAsync();
        }

        private async Task LoadLobbiesAsync() {
            try {
                // Llamada al servicio para obtener los lobbies disponibles
                var lobbies = lobbyManager.GetAvailableLobbies();
                LobbyDataGrid.ItemsSource = lobbies;
            } catch (Exception ex) {
                MessageBox.Show($"Error al cargar los lobbies: {ex.Message}");
            }
        }


        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            // Aquí puedes manejar la selección de un lobby si es necesario
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
                    bool joined = await lobbyManager.JoinLobbyAsync(lobbyCode, guest);

                    if (joined) {
                        // Navegar a LobbyView si la unión fue exitosa
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
            // Lógica para volver a la pantalla anterior
        }
    }
}
