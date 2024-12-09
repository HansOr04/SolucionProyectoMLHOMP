using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace APPProyectoMLHOMP.Models
{
    public class User : INotifyPropertyChanged
    {
        private int userId;
        private string firstName = string.Empty;
        private string lastName = string.Empty;
        private DateTime dateOfBirth;
        private string address = string.Empty;
        private string city = string.Empty;
        private string country = string.Empty;
        private string email = string.Empty;
        private string phoneNumber = string.Empty;
        private string username = string.Empty;
        private string passwordHash = string.Empty;
        private DateTime registrationDate;
        private bool isHost;
        private bool isVerified;
        private string biography = string.Empty;
        private string languages = string.Empty;
        private string profileImageUrl = "/images/default-profile.jpg";

        public event PropertyChangedEventHandler? PropertyChanged;

        public int UserId
        {
            get => userId;
            set => SetProperty(ref userId, value);
        }

        public string FirstName
        {
            get => firstName;
            set => SetProperty(ref firstName, value);
        }

        public string LastName
        {
            get => lastName;
            set => SetProperty(ref lastName, value);
        }

        public DateTime DateOfBirth
        {
            get => dateOfBirth;
            set => SetProperty(ref dateOfBirth, value);
        }

        public string Address
        {
            get => address;
            set => SetProperty(ref address, value);
        }

        public string City
        {
            get => city;
            set => SetProperty(ref city, value);
        }

        public string Country
        {
            get => country;
            set => SetProperty(ref country, value);
        }

        public string Email
        {
            get => email;
            set => SetProperty(ref email, value);
        }

        public string PhoneNumber
        {
            get => phoneNumber;
            set => SetProperty(ref phoneNumber, value);
        }

        public string Username
        {
            get => username;
            set => SetProperty(ref username, value);
        }

        public string PasswordHash
        {
            get => passwordHash;
            set => SetProperty(ref passwordHash, value);
        }

        private string password = string.Empty;
        public string Password
        {
            get => password;
            set => SetProperty(ref password, value);
        }

        public DateTime RegistrationDate
        {
            get => registrationDate;
            set => SetProperty(ref registrationDate, value);
        }

        public bool IsHost
        {
            get => isHost;
            set => SetProperty(ref isHost, value);
        }

        public bool IsVerified
        {
            get => isVerified;
            set => SetProperty(ref isVerified, value);
        }

        public string Biography
        {
            get => biography;
            set => SetProperty(ref biography, value);
        }

        public string Languages
        {
            get => languages;
            set => SetProperty(ref languages, value);
        }

        public string ProfileImageUrl
        {
            get => profileImageUrl;
            set => SetProperty(ref profileImageUrl, value);
        }

        // Métodos auxiliares
        public string GetFullName() => $"{FirstName} {LastName}";

        public bool VerifyPassword(string password)
        {
            return HashPassword(password) == this.PasswordHash;
        }

        public static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        // Implementación de INotifyPropertyChanged
        protected bool SetProperty<T>(ref T field, T newValue, [CallerMemberName] string? propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, newValue))
            {
                field = newValue;
                OnPropertyChanged(propertyName);
                return true;
            }
            return false;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}