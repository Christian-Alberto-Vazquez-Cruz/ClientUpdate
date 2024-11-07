using System.ServiceModel;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TripasDeGatoCliente.Logic;
using TripasDeGatoCliente.Properties;
using TripasDeGatoCliente.TripasDeGatoServicio;
using static TripasDeGatoCliente.Logic.ConstantsManager;
using System.Xml.Linq;
using System.Linq;

namespace TripasDeGatoCliente.Views {
    public partial class LoginView : Page {
        public LoginView() {
            InitializeComponent();
            UpdatePasswordVisibilityIcon();
            txtEmail.TextChanged += TxtEmail_TextChanged;
            txtPassword.PasswordChanged += TxtPassword_PasswordChanged;
        }


        private string userEmail;
        private void BtnLogin_Click(object sender, RoutedEventArgs e) {
            txtEmail.BorderBrush = new SolidColorBrush(Colors.White);
            txtPassword.BorderBrush = new SolidColorBrush(Colors.White);

            if (string.IsNullOrWhiteSpace(txtEmail.Text)) {
                txtEmail.BorderBrush = Brushes.Red;
                DialogManager.ShowErrorMessageAlert(Properties.Resources.dialogInvalidEmail);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPassword.Password)) {
                txtPassword.BorderBrush = Brushes.Red;
                DialogManager.ShowErrorMessageAlert(Properties.Resources.dialogInvalidPassword);
                return;
            }

            string mail = txtEmail.Text;
            string password = Hasher.HashToSHA256(txtPassword.Password);

            if (VerifyFields()) {
                if (ValidateCredentials(mail, password)) {
                    SetPlayerOnlineStatus(UserProfileSingleton.IdPerfil);
                    DisplayMainMenuView();
                } else {
                    DialogManager.ShowErrorMessageAlert(Properties.Resources.dialogMissmatchesCredentials);
                }
            } else {
                DialogManager.ShowErrorMessageAlert(Properties.Resources.dialogWrongData);
            }
        }
        private void TxtEmail_TextChanged(object sender, TextChangedEventArgs e) {
            txtEmail.BorderBrush = new SolidColorBrush(Colors.White);
        }

        private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e) {
            txtPassword.BorderBrush = new SolidColorBrush(Colors.White);
            UpdatePasswordVisibilityIcon();
        }

        private bool VerifyFields() {
            bool passwordValid = Validador.ValidatePassword(txtPassword.Password);
            bool emailValid = Validador.ValidateEmail(txtEmail.Text);

            if (!passwordValid) {
                txtPassword.BorderBrush = Brushes.Red;
            }
            if (!emailValid) {
                txtEmail.BorderBrush = Brushes.Red;
            }

            return passwordValid && emailValid;
        }

        public bool ValidateCredentials(string mail, string password) {
            bool validateResult = false;
            LoggerManager logger = new LoggerManager(this.GetType());
            int validationResult = 0;

            try {
                IUserManager userManager = new UserManagerClient();
                validationResult = userManager.VerifyLogin(mail, password);

                if (validationResult == Constants.DATA_MATCHES) {
                    Profile profile = userManager.GetProfileByMail(mail);
                    if (profile != null) {
                        ObtainSingletonData(profile);
                        validateResult = true;
                    } else {
                        DialogManager.ShowErrorMessageAlert(Properties.Resources.dialogNotRetrievedProfile);
                    }
                } else {
                    DialogManager.ShowErrorMessageAlert(Properties.Resources.dialogMissmatchesCredentials);
                }
            } catch (Exception ex) {
                logger.LogError(ex);
                DialogManager.ShowErrorMessageAlert(Properties.Resources.dialogConnectionErrorWithServer);
            }

            return validateResult;
        }

        private void ObtainSingletonData(Profile profile) {
            UserProfileSingleton.Instance.CreateInstance(profile);
        }

        private void DisplayMainMenuView() {
            MenuView menuView = new MenuView();
            this.NavigationService.Navigate(menuView);
        }

        private void BtnSignIn_Click(object sender, RoutedEventArgs e) {
            RegisterView registerView = new RegisterView();
            this.NavigationService.Navigate(registerView);
        }
        private void BtnGuest_Click(object sender, RoutedEventArgs e) {

        }

        private void BtnTogglePassword_Checked(object sender, RoutedEventArgs e) {
            txtPasswordVisible.Text = txtPassword.Password;
            txtPasswordVisible.Visibility = Visibility.Visible;
            txtPassword.Visibility = Visibility.Collapsed;
        }

        private void BtnTogglePassword_Unchecked(object sender, RoutedEventArgs e) {
            txtPassword.Password = txtPasswordVisible.Text;
            txtPasswordVisible.Visibility = Visibility.Collapsed;
            txtPassword.Visibility = Visibility.Visible;
        }

        private void UpdatePasswordVisibilityIcon() {
            if (txtPassword.Password.Length > 0) {
                btnTogglePassword.Visibility = Visibility.Visible;
            } else {
                btnTogglePassword.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnRecoverPassword_Click(object sender, RoutedEventArgs e) {
            enterEmailGrid.Visibility = Visibility.Visible;
        }
        private void BtnContinue_Click(object sender, RoutedEventArgs e) {
            string email = txtEmailRecovery.Text.Trim();

            if (string.IsNullOrWhiteSpace(email)) {
                DialogManager.ShowErrorMessageAlert("Por favor, ingrese un correo electrónico.");
                return;
            }

            if (!Validador.ValidateEmail(email)) {
                DialogManager.ShowErrorMessageAlert("El correo electrónico ingresado no es válido.");
                return;
            }

            IPasswordRecoveryManager passwordRecovery = new PasswordRecoveryManagerClient();

            int result = passwordRecovery.SendRecoveryCode(email);

            if (result == Constants.SUCCES_OPERATION) {
                userEmail = email;
                enterEmailGrid.Visibility = Visibility.Collapsed;
                validationGrid.Visibility = Visibility.Visible;
                DialogManager.ShowSuccessMessageAlert("Se ha enviado un código de recuperación a su correo.");
            } else if (result == Constants.NO_DATA_MATCHES) {
                DialogManager.ShowErrorMessageAlert("El correo electrónico no está registrado.");
            } else {
                DialogManager.ShowErrorMessageAlert("Error al enviar el código de recuperación.");
            }
        }
        private void BtnBackEnterEmail_Click(object sender, RoutedEventArgs e) {
            enterEmailGrid.Visibility = Visibility.Collapsed;
            txtEmail.Clear();
            txtPassword.Clear();
            txtPasswordVisible.Clear();
        }
        private void BtnValidate_Click(object sender, RoutedEventArgs e) {
            string enteredCode = $"{txtValidationCode1.Text}{txtValidationCode2.Text}{txtValidationCode3.Text}{txtValidationCode4.Text}{txtValidationCode5.Text}{txtValidationCode6.Text}";

            IPasswordRecoveryManager passwordRecovery = new PasswordRecoveryManagerClient();

            bool isCodeValid = passwordRecovery.VerifyRecoveryCode(userEmail, enteredCode);

            if (isCodeValid) {
                validationGrid.Visibility = Visibility.Collapsed;
                recoveryPasswordGrid.Visibility = Visibility.Visible;
            } else {
                DialogManager.ShowErrorMessageAlert("Código de verificación inválido. Por favor, inténtelo de nuevo.");
            }
        }
        private void BtnResendCode_Click(object sender, RoutedEventArgs e) {
            if (string.IsNullOrEmpty(userEmail)) {
                DialogManager.ShowErrorMessageAlert("No se ha ingresado un correo electrónico.");
                return;
            }

            var emailVerificationProxy = new TripasDeGatoServicio.PasswordRecoveryManagerClient();
            int result = emailVerificationProxy.SendRecoveryCode(userEmail);
            if (result == Constants.SUCCES_OPERATION) {
                DialogManager.ShowSuccessMessageAlert("Código de verificación reenviado exitosamente. Por favor, revise su correo.");
            } else {
                DialogManager.ShowErrorMessageAlert("Error al reenviar el código de verificación. Por favor, intente nuevamente.");
            }
        }

        private void BtnBackValidate_Click(object sender, RoutedEventArgs e) {
            validationGrid.Visibility = Visibility.Collapsed;
            txtEmail.Clear();
            txtPassword.Clear();
            txtPasswordVisible.Clear();
        }
        private void BtnBackRecovery_Click(object sender, RoutedEventArgs e) {
            recoveryPasswordGrid.Visibility = Visibility.Collapsed;
            txtEmail.Clear();
            txtPassword.Clear();
            txtPasswordVisible.Clear();
        }

        private void TxtValidationCode_TextChanged(object sender, TextChangedEventArgs e) {
            TextBox currentTextBox = sender as TextBox;
            if (currentTextBox.Text.Length == 1) {
                if (currentTextBox == txtValidationCode1)
                    txtValidationCode2.Focus();
                else if (currentTextBox == txtValidationCode2)
                    txtValidationCode3.Focus();
                else if (currentTextBox == txtValidationCode3)
                    txtValidationCode4.Focus();
                else if (currentTextBox == txtValidationCode4)
                    txtValidationCode5.Focus();
                else if (currentTextBox == txtValidationCode5)
                    txtValidationCode6.Focus();
            }
        }
        private void BtnSavePassword_Click(object sender, RoutedEventArgs e) {
            string newPassword = txtNewPassword.Password.Trim();
            string confirmPassword = txtNewPasswordConfirm.Password.Trim();
            string email = txtEmail.Text.Trim();

            if (newPassword != confirmPassword) {
                DialogManager.ShowErrorMessageAlert("Las contraseñas no coinciden. Por favor, inténtelo de nuevo.");
                return;
            }

            if (!IsValidPassword(newPassword)) {
                DialogManager.ShowErrorMessageAlert("La nueva contraseña no cumple con los requisitos de seguridad.");
                return;
            }

            IPasswordRecoveryManager passwordRecovery = new PasswordRecoveryManagerClient();
            int result = passwordRecovery.UpdatePassword(userEmail, Hasher.HashToSHA256(newPassword));

            if (result == Constants.SUCCES_OPERATION) {
                DialogManager.ShowSuccessMessageAlert("La contraseña ha sido actualizada exitosamente.");
                txtNewPassword.Clear();
                txtNewPasswordConfirm.Clear();
            } else {
                DialogManager.ShowErrorMessageAlert($"Error al actualizar la contraseña. Por favor, inténtelo de nuevo. {email}, {newPassword}");
            }
        }
        private bool IsValidPassword(string password) {
            bool isValid = true;
            if (!Validador.ValidatePassword(password)) {
                isValid = false;
            }
            return isValid;
        }
        private void BtnToggleNewPassword_Checked(object sender, RoutedEventArgs e) {
            txtNewPasswordVisible.Text = txtNewPassword.Password;
            txtNewPasswordVisible.Visibility = Visibility.Visible;
            txtNewPassword.Visibility = Visibility.Collapsed;

            txtNewPasswordConfirmVisible.Text = txtNewPasswordConfirm.Password;
            txtNewPasswordConfirmVisible.Visibility = Visibility.Visible;
            txtNewPasswordConfirm.Visibility = Visibility.Collapsed;
        }

        private void BtnToggleNewPassword_Unchecked(object sender, RoutedEventArgs e) {
            txtNewPassword.Password = txtNewPasswordVisible.Text;
            txtNewPassword.Visibility = Visibility.Visible;
            txtNewPasswordVisible.Visibility = Visibility.Collapsed;

            txtNewPasswordConfirm.Password = txtNewPasswordConfirmVisible.Text;
            txtNewPasswordConfirm.Visibility = Visibility.Visible;
            txtNewPasswordConfirmVisible.Visibility = Visibility.Collapsed;
        }


        //NUEVO
        private void SetPlayerOnlineStatus(int playerId) {
            try {
                IStatusManager statusManager = new StatusManagerClient();
                statusManager.SetPlayerStatus(playerId, GameEnumsPlayerStatus.Online);
            } catch (Exception ex) {
                // Manejo de errores, tal vez quieras registrar el error
                LoggerManager logger = new LoggerManager(this.GetType());
                logger.LogError(ex);
                DialogManager.ShowErrorMessageAlert("Error al actualizar el estado del jugador.");
            }
        }

    }
}