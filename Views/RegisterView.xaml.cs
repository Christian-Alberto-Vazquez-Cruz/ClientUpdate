using log4net;
using System.ServiceModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TripasDeGatoCliente.Logic;
using TripasDeGatoCliente.TripasDeGatoServicio;
using static TripasDeGatoCliente.Logic.ConstantsManager;

namespace TripasDeGatoCliente.Views
{
    public partial class RegisterView : Page
    {
        public RegisterView()
        {
            InitializeComponent();
        }

        private void BtnSignIn_Click(object sender, RoutedEventArgs e)
        {
            string email = txtEmail.Text;
            string username = txtName.Text;
            string password = txtPassword.Password;

            if (!ValidateFields(email, username, password))
            {
                return;
            }

            var userProxy = new TripasDeGatoServicio.UserManagerClient();

            bool emailRegistered = userProxy.IsEmailRegistered(email);
            if (emailRegistered)
            {
                DialogManager.ShowErrorMessageAlert(Properties.Resources.dialogEmailInUse);
                HighlightField(txtEmail);
                return;
            }

            var emailVerificationProxy = new TripasDeGatoServicio.EmailVerificationManagerClient();
            int result = emailVerificationProxy.SendVerificationCodeRegister(email);

            if (result == Constants.SUCCES_OPERATION)
            {
                validationGrid.Visibility = Visibility.Visible;
                DialogManager.ShowSuccessMessageAlert("Verification code sent successfully. Please check your email.");
            }
            else
            {
                DialogManager.ShowErrorMessageAlert("Error sending verification code. Please try again.");
            }
        }

        private void BtnResendCode_Click(object sender, RoutedEventArgs e)
        {
            var emailVerificationProxy = new TripasDeGatoServicio.EmailVerificationManagerClient();
            int result = emailVerificationProxy.SendVerificationCodeRegister(txtEmail.Text);

            if (result == Constants.SUCCES_OPERATION)
            {
                DialogManager.ShowSuccessMessageAlert("Verification code resent successfully. Please check your email.");
            }
            else
            {
                DialogManager.ShowErrorMessageAlert("Error resending verification code. Please try again.");
            }
        }

        private void BtnValidate_Click(object sender, RoutedEventArgs e)
        {
            string enteredCode = $"{txtValidationCode1.Text}{txtValidationCode2.Text}{txtValidationCode3.Text}{txtValidationCode4.Text}{txtValidationCode5.Text}{txtValidationCode6.Text}";

            var emailVerificationProxy = new TripasDeGatoServicio.EmailVerificationManagerClient();
            bool isCodeValid = emailVerificationProxy.VerifyCode(txtEmail.Text, enteredCode);

            if (isCodeValid)
            {
                var userProxy = new TripasDeGatoServicio.UserManagerClient();

                var newUser = new TripasDeGatoServicio.LoginUser
                {
                    mail = txtEmail.Text,
                    password = Hasher.HashToSHA256(txtPassword.Password)
                };

                var newProfile = new TripasDeGatoServicio.Profile
                {
                    userName = txtName.Text,
                    score = Constants.INITIAL_SCORE,
                    picturePath = "/Images/DefaultImageProfile"
                };

                int accountResult = userProxy.CreateAccount(newUser, newProfile);

                if (accountResult == Constants.SUCCES_OPERATION)
                {
                    DialogManager.ShowSuccessMessageAlert(Properties.Resources.dialogAccountCreatedSuccesfully);
                    GoToLoginView();
                }
                else
                {
                    DialogManager.ShowErrorMessageAlert(Properties.Resources.dialogAccountCreatedErrror);
                }
            }
            else
            {
                DialogManager.ShowErrorMessageAlert("Invalid verification code. Please try again.");
            }
        }


        private bool ValidateFields(string email, string username, string password)
        {
            bool isValid = true;

            if (!Validador.ValidateEmail(email))
            {
                HighlightField(txtEmail);
                lbInvalidEmail.Visibility = Visibility.Visible;
                isValid = false;
            }

            if (!Validador.ValidateUsername(username))
            {
                HighlightField(txtName);
                lbInvalidUser.Visibility = Visibility.Visible;
                isValid = false;
            }

            if (!Validador.ValidatePassword(password))
            {
                HighlightField(txtPassword);
                lbInvalidPassword.Visibility = Visibility.Visible;
                isValid = false;
            }
            else
            {
                ResetField(txtPassword);
                lbInvalidPassword.Visibility = Visibility.Collapsed;
            }

            return isValid;
        }

        private void HighlightField(Control control)
        {
            control.BorderBrush = Brushes.Red;
        }

        private void GoToLoginView()
        {
            if (this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
        }

        private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            string password = txtPassword.Password;

            if (!Validador.ValidatePassword(password))
            {
                HighlightField(txtPassword);
                lbInvalidPassword.Visibility = Visibility.Visible;
                txtPassword.ToolTip = "Password does not meet the criteria";
            }
            else
            {
                ResetField(txtPassword);
                lbInvalidPassword.Visibility = Visibility.Collapsed;

                txtPassword.ToolTip = null;
            }

            UpdatePasswordVisibilityIcon();
        }
        private void ResetField(Control control)
        {
            control.BorderBrush = Brushes.White;
        }

        private void TxtName_TextChanged(object sender, TextChangedEventArgs e)
        {
            string username = txtName.Text;
            if (!Validador.ValidateUsername(username))
            {
                HighlightField(txtName);
                lbInvalidUser.Visibility = Visibility.Visible;
            }
            else
            {
                txtName.BorderBrush = Brushes.White;
                lbInvalidUser.Visibility = Visibility.Collapsed;
                txtName.ToolTip = null;
            }
        }

        private void TxtEmail_TextChanged(object sender, TextChangedEventArgs e)
        {
            string email = txtEmail.Text;
            if (!Validador.ValidateEmail(email))
            {
                HighlightField(txtEmail);
                lbInvalidEmail.Visibility = Visibility.Visible;
            }
            else
            {
                txtEmail.BorderBrush = Brushes.White;
                lbInvalidEmail.Visibility = Visibility.Collapsed;
                txtEmail.ToolTip = null;
            }
        }


        private void BtnTogglePassword_Checked(object sender, RoutedEventArgs e)
        {
            txtPasswordVisible.Text = txtPassword.Password;
            txtPasswordVisible.Visibility = Visibility.Visible;
            txtPassword.Visibility = Visibility.Collapsed;
        }

        private void BtnTogglePassword_Unchecked(object sender, RoutedEventArgs e)
        {
            txtPassword.Password = txtPasswordVisible.Text;
            txtPasswordVisible.Visibility = Visibility.Collapsed;
            txtPassword.Visibility = Visibility.Visible;
        }

        private void UpdatePasswordVisibilityIcon()
        {
            if (txtPassword.Password.Length > 0)
            {
                btnTogglePassword.Visibility = Visibility.Visible;
            }
            else
            {
                btnTogglePassword.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            GoToLoginView();
        }
        private void BtnBackValidate_Click(object sender, RoutedEventArgs e)
        {
            validationGrid.Visibility = Visibility.Collapsed;
            txtEmail.Clear();
            txtName.Clear();
            txtPassword.Clear();
            txtPasswordVisible.Clear();
        }
        private void TxtValidationCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox currentTextBox = sender as TextBox;
            if (currentTextBox.Text.Length == 1)
            {
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

    }
}
