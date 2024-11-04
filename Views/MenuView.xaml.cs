using System.ServiceModel;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TripasDeGatoCliente.Logic;
using static TripasDeGatoCliente.Logic.ConstantsManager;
using TripasDeGatoCliente.TripasDeGatoServicio;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace TripasDeGatoCliente.Views {
    public partial class MenuView : Page {
        private UserManagerClient userManager;
        private FriendsManagerClient friendsManager;
        private StatusManagerClient statusManager;
        private LobbyManagerClient lobbyManager;

        public MenuView() {
            InitializeComponent();
            userManager = new UserManagerClient();
            friendsManager = new FriendsManagerClient();
            statusManager = new StatusManagerClient();
            lobbyManager = new LobbyManagerClient();
            LoadUserProfileAsync();
        }

        private async void LoadUserProfileAsync() {
            if (!string.IsNullOrEmpty(UserProfileSingleton.Nombre)) {
                lbUserName.Content = UserProfileSingleton.Nombre;
            } else {
                lbUserName.Content = "Usuario desconocido";
            }
        }

        private void BtnSignOut_Click(object sender, RoutedEventArgs e) {
            UserProfileSingleton.Instance.ResetInstance();
            LoginView loginView = new LoginView();
            this.NavigationService.Navigate(loginView);
        }

        private bool areElementsVisible = false;

        private async void BtnFriends_Click(object sender, RoutedEventArgs e) {
            areElementsVisible = !areElementsVisible;

            if (areElementsVisible) {
                lstFriends.Visibility = Visibility.Visible;
                btnAddFriend.Visibility = Visibility.Visible;
                btnRemoveFriend.Visibility = Visibility.Visible;
                await LoadFriendsListAsync();

                lstFriends.IsEnabled = true;
                btnAddFriend.IsEnabled = true;
                btnRemoveFriend.IsEnabled = true;

                btnFriends.Background = new SolidColorBrush(Colors.Green);
            } else {
                lstFriends.Visibility = Visibility.Collapsed;
                btnAddFriend.Visibility = Visibility.Collapsed;
                btnRemoveFriend.Visibility = Visibility.Collapsed;
                txtFriendName.Visibility = Visibility.Collapsed;
                btnAdd.Visibility = Visibility.Collapsed;

                lstFriends.IsEnabled = false;
                btnAddFriend.IsEnabled = false;
                btnRemoveFriend.IsEnabled = false;
                txtFriendName.IsEnabled = false;
                btnAdd.IsEnabled = false;

                btnFriends.Background = new SolidColorBrush(Colors.Black);
                btnAddFriend.Background = new SolidColorBrush(Colors.Black);
            }
        }

        private void BtnAddFriend_Click(object sender, RoutedEventArgs e) {
            areElementsVisible = !areElementsVisible;

            if (areElementsVisible) {
                txtFriendName.Visibility = Visibility.Visible;
                btnAdd.Visibility = Visibility.Visible;
                txtFriendName.IsEnabled = true;
                btnAdd.IsEnabled = true;

                btnAddFriend.Background = new SolidColorBrush(Colors.Green);
            } else {
                txtFriendName.Visibility = Visibility.Collapsed;
                btnAdd.Visibility = Visibility.Collapsed;

                txtFriendName.IsEnabled = false;
                btnAdd.IsEnabled = false;

                btnAddFriend.Background = new SolidColorBrush(Colors.Black);
            }
        }

        private async void BtnAdd_Click(object sender, RoutedEventArgs e) {
            string friendName = txtFriendName.Text.Trim();

            if (!string.IsNullOrEmpty(friendName)) {
                try {
                    int friendProfileId = await userManager.getProfileIdAsync(friendName);
                    int userProfileId = UserProfileSingleton.IdPerfil;

                    if (friendProfileId == userProfileId) {
                        MessageBox.Show("No puedes agregarte a ti mismo como amigo.");
                    } else if (friendProfileId > 0) {
                        int result = await friendsManager.addFriendAsync(userProfileId, friendProfileId);

                        if (result == Constants.SUCCES_OPERATION) {
                            MessageBox.Show($"Amigo '{friendName}' agregado correctamente.");
                            await LoadFriendsListAsync();
                        } else {
                            MessageBox.Show("Error al agregar el amigo. Intenta de nuevo.");
                        }
                    } else {
                        MessageBox.Show("No se encontró un perfil que coincida con ese nombre.");
                    }

                    txtFriendName.Clear();
                } catch (FaultException<ProfileNotFoundFault> ex) {
                    MessageBox.Show(ex.Detail.errorMessage, "Perfil no encontrado");
                } catch (Exception ex) {
                    MessageBox.Show($"Ocurrió un error: {ex.Message}");
                }
            } else {
                MessageBox.Show("Por favor, ingresa un nombre válido.");
            }
        }

        private async Task LoadFriendsListAsync() {
            try {
                int userProfileId = UserProfileSingleton.IdPerfil;
                var friendsList = await friendsManager.getFriendsAsync(userProfileId);

                var friendsWithStatus = new List<string>();

                foreach (var friend in friendsList) {
                    var status = await statusManager.GetPlayerStatusAsync(friend.idProfile);
                    friendsWithStatus.Add($"{friend.userName} - {status}");
                }

                lstFriends.ItemsSource = friendsWithStatus;
            } catch (Exception ex) {
                MessageBox.Show($"Ocurrió un error al cargar la lista de amigos: {ex.Message}");
            }
        }

        private async void BtnRemoveFriend_Click(object sender, RoutedEventArgs e) {
            if (lstFriends.SelectedItem != null) {
                string selectedFriendName = lstFriends.SelectedItem.ToString();

                try {
                    int friendProfileId = await userManager.getProfileIdAsync(selectedFriendName);

                    if (friendProfileId > 0) {
                        int userProfileId = UserProfileSingleton.IdPerfil;
                        int result = await friendsManager.deleteFriendAsync(userProfileId, friendProfileId);

                        if (result == Constants.SUCCES_OPERATION) {
                            MessageBox.Show($"Amistad con '{selectedFriendName}' eliminada correctamente.");
                            await LoadFriendsListAsync();
                        } else {
                            MessageBox.Show("Error al eliminar la amistad. Intenta de nuevo.");
                        }
                    } else {
                        MessageBox.Show("No se encontró el perfil del amigo seleccionado.");
                    }
                } catch (Exception ex) {
                    MessageBox.Show($"Ocurrió un error al intentar eliminar la amistad: {ex.Message}");
                }
            } else {
                MessageBox.Show("Por favor, selecciona un amigo para eliminar.");
            }
        }

        private async void BtnStartGame_Click(object sender, RoutedEventArgs e) {
            string gameName = "NombreDelJuego";
            int nodeCount = 2;
            Profile owner = new Profile { idProfile = UserProfileSingleton.IdPerfil, userName = UserProfileSingleton.Nombre };

            try {
                string lobbyCode = await lobbyManager.CreateLobbyAsync(gameName, nodeCount, owner);

                if (!string.IsNullOrEmpty(lobbyCode)) {
                    GoToLobbyView(lobbyCode);
                } else {
                    MessageBox.Show("Error al crear el lobby.");
                }
            } catch (Exception ex) {
                MessageBox.Show($"Ocurrió un error al crear el lobby: {ex.Message}");
            }
        }

        private void GoToLobbyView(string lobbyCode) {
            LobbyView lobbyView = new LobbyView(lobbyCode);
            if (this.NavigationService != null) {
                this.NavigationService.Navigate(lobbyView);
            } else {
                MessageBox.Show("Error: No se puede navegar a LobbyView.");
            }
        }

        private void GoToPerfilView() {
            ProfileView profileView = new ProfileView();
            if (this.NavigationService != null) {
                this.NavigationService.Navigate(profileView);
            } else {
                MessageBox.Show("Error: No se puede navegar al menu.");
            }
        }

        private void BtnProfile_Click(object sender, RoutedEventArgs e) {
            GoToPerfilView();
        }

        private void btnJoinGame_Click(object sender, RoutedEventArgs e) {
            SelectLobbyView selectLobbyView = new SelectLobbyView();
            if (this.NavigationService != null) {
                this.NavigationService.Navigate(selectLobbyView);
            } else {
                MessageBox.Show("Error: No se puede navegar a SelectLobbyView.");
            }
        }
    }
}
