using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using TMPro;
using UnityEngine.SceneManagement;
using Firebase.Database;
using Photon.Pun;
using UnityEngine.UI;


public class FirebaseManager : MonoBehaviour
{

    //Firebase variables
    [Header("Firebase")]
    public DependencyStatus dependencyStatus;
    public FirebaseAuth auth;
    public static FirebaseUser User;
    public DatabaseReference DBReference;
    private static FirebaseManager _singleton;

    public bool hasFixedDependencies = false;

    //static int hasLaunched = 0;

    //SceneTracker sceneTracker;

    public string playerColorValue;

    public int currentXP;
    public int currentLVL;

    public ExitGames.Client.Photon.Hashtable _customProps = new ExitGames.Client.Photon.Hashtable();

    string settingsString;
    List<string> settingsArray = new();

    GameObject volume;

    public bool isMusicOn;
    public bool lowQuality;
    public bool crosshairs;
    public bool alwaysSprint;

    private void Awake()
    {
        Singleton = this;
        DontDestroyOnLoad(this.gameObject);
        Debug.Log(PhotonNetwork.NickName);
    }


    public static FirebaseManager Singleton
    {
        get => _singleton;
        private set
        {
            if (_singleton == null)
            {
                _singleton = value;
            }
            else if (_singleton != value)
            {
                Debug.Log($"{nameof(FirebaseManager)} instance already exists, destroying object!");
                Destroy(value.gameObject);
            }
            Debug.Log("Singleton called");
        }
    }

    private IEnumerator CheckAndFixDependencies()
    {
        var checkAndFixDependenciesTask = FirebaseApp.CheckAndFixDependenciesAsync();

        yield return new WaitUntil(predicate: () => checkAndFixDependenciesTask.IsCompleted);

        var dependencyResult = checkAndFixDependenciesTask.Result;

        if (dependencyResult == DependencyStatus.Available)
        {
            InitializeFirebase();
        }
        else
        {
            Debug.LogError($"Couldn't resolve all Firebase Dependencies: {dependencyResult}");
        }
    }

    private void InitializeFirebase()
    {
        Debug.Log("Setting up Firebase Auth");
        //Set the authentication instance object
        auth = FirebaseAuth.DefaultInstance;
        DBReference = FirebaseDatabase.DefaultInstance.RootReference;
        StartCoroutine(CheckAutoLogin());

        auth.StateChanged += AuthStateChanged;
        AuthStateChanged(this, null);
    }

    IEnumerator CheckAutoLogin()
    {
        yield return new WaitForEndOfFrame();
        if (User != null)
        {
            var reloadTask = User.ReloadAsync();

            yield return new WaitUntil(predicate: () => reloadTask.IsCompleted);

            AutoLogin();
        }
        else
        {
            AccountUIManager.instance.accountCanvas.SetActive(true);
            AccountUIManager.instance.LoginScreen();
        }
    }

    void AutoLogin()
    {
        if (User != null)
        {
            if (User.IsEmailVerified)
            {
                AccountUIManager.instance.menuCanvas.SetActive(true);
                AccountUIManager.instance.accountCanvas.SetActive(false);
                Debug.Log("Should be logged in...");
                LoadAllInitialDatabaseInfo();
            }
            else
            {
                StartCoroutine(SendVerificationEmail());
            }
        }
        else
        {
            AccountUIManager.instance.accountCanvas.SetActive(true);
            AccountUIManager.instance.LoginScreen();
            Debug.Log("Shoud NOT be logged in...");
        }
                
    }

    public void SaveUsernameData()
    {
        if (AccountUIManager.instance.usernameInputField.text == null)
            return;
        StartCoroutine(UpdateUsernameAuth(AccountUIManager.instance.usernameInputField.text));
        StartCoroutine(UpdateUsernameDatabase(AccountUIManager.instance.usernameInputField.text));
        StartCoroutine(LoadUsernameData());
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name == "Menu")
        {
            if (Time.timeSinceLevelLoad < Mathf.Epsilon) 
            {
                StartCoroutine(CheckAndFixDependencies());
                volume = GameObject.Find("Post-process Volume");

            }

            if (User == null)
                return;
            AccountUIManager.instance.accountDetailsEmail.text = "Your email is: " + User.Email;
        }
        if (SceneManager.GetActiveScene().name != "Menu")
        {
            if (User == null)
                return;
            if (Time.timeSinceLevelLoad < Mathf.Epsilon)
            {
                StartCoroutine(LoadSettings());
                volume = GameObject.Find("Post-process Volume");
            }
        }

        if (User != null)
        {
            hasFixedDependencies = true;
        }
    }

    private void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        if (auth.CurrentUser != User)
        {
            bool signedIn = User != auth.CurrentUser && auth.CurrentUser != null;

            if (!signedIn && User != null)
            {
                Debug.Log("signed out");
            }

            User = auth.CurrentUser;

            if (signedIn)
            {
                Debug.Log($"Signed In: { User.DisplayName }");
            }
        }
    }

    //Function for the login button
    public void LoginButton()
    {
        //Call the login coroutine passing the email and password
        StartCoroutine(Login(AccountUIManager.instance.emailLoginField.text, AccountUIManager.instance.passwordLoginField.text));
    }
    //Function for the register button
    public void RegisterButton()
    {
        //Call the register coroutine passing the email, password, and username
        StartCoroutine(Register(AccountUIManager.instance.emailRegisterField.text, AccountUIManager.instance.passwordRegisterField.text, AccountUIManager.instance.usernameRegisterField.text));
    }
    private IEnumerator Login(string _email, string _password)
    {
        Credential credential = EmailAuthProvider.GetCredential(_email, _password);

        var LoginTask = auth.SignInWithCredentialAsync(credential);

        //var LoginTask = auth.SignInWithEmailAndPasswordAsync(_email, _password);
        yield return new WaitUntil(predicate: () => LoginTask.IsCompleted);

        if (LoginTask.Exception != null)
        {
            //If there are errors handle them
            Debug.LogWarning(message: $"Failed to register task with {LoginTask.Exception}");
            FirebaseException firebaseEx = LoginTask.Exception.GetBaseException() as FirebaseException;
            AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

            string message = "Login Failed!";
            switch (errorCode)
            {
                case AuthError.MissingEmail:
                    message = "Missing Email";
                    break;
                case AuthError.MissingPassword:
                    message = "Missing Password";
                    break;
                case AuthError.WrongPassword:
                    message = "Wrong Password";
                    break;
                case AuthError.InvalidEmail:
                    message = "Invalid Email";
                    break;
                case AuthError.UserNotFound:
                    message = "Account does not exist";
                    break;
            }
            AccountUIManager.instance.warningLoginText.text = message;
        }
        else
        {
            if (User.IsEmailVerified)
            {
                //User is now logged in
                //Now get the result
                User = LoginTask.Result;
                Debug.LogFormat("User signed in successfully: {0} ({1})", User.DisplayName, User.Email);
                AccountUIManager.instance.warningLoginText.text = "";
                AccountUIManager.instance.confirmLoginText.text = "Logged In";
                LoadAllInitialDatabaseInfo();

                yield return new WaitForSeconds(2);

                //SceneManager.LoadScene(1);
                AccountUIManager.instance.menuCanvas.SetActive(true);
                AccountUIManager.instance.accountCanvas.SetActive(false);
                AccountUIManager.instance.titleMenu.SetActive(true);
                AccountUIManager.instance.mainCamera.transform.Find("PlayerViewer").gameObject.SetActive(true);
                AccountUIManager.instance.confirmLoginText.text = "";
                AccountUIManager.instance.emailLoginField.text = "";
                AccountUIManager.instance.passwordLoginField.text = "";

                //if (PhotonNetwork.IsConnectedAndReady)
                //    Debug.Log("Connected TEST DEBUG");
            }
            else
            {
                StartCoroutine(SendVerificationEmail());
            }
        }
    }

    public void LoadAllInitialDatabaseInfo()
    {
        if (SceneManager.GetActiveScene().name == "Menu")
        {
            StartCoroutine(LoadUsernameData());
            StartCoroutine(LoadSettings());
            StartCoroutine(LoadExperience());
            StartCoroutine(LoadApplicationGenuinity());
            StartCoroutine(LoadKills());
            StartCoroutine(CheckForRemoveHat());
            StartCoroutine(CheckForRemoveEyeWear());
            StartCoroutine(CheckForRemoveCape());
            StartCoroutine(LoadHats());
            StartCoroutine(LoadEyewear());
            StartCoroutine(LoadCapes());
            StartCoroutine(LoadPlayerColorDataMainMenuBeanModel(AccountUIManager.instance.mainMenuBeanObject, AccountUIManager.instance.fcp, AccountUIManager.instance.healthyMat));
        }
    }

    private IEnumerator Register(string _email, string _password, string _username)
    {
        if (_username == "")
        {
            //If the username field is blank show a warning
            AccountUIManager.instance.warningRegisterText.text = "Missing Username";
        }
        else if (AccountUIManager.instance.passwordRegisterField.text != AccountUIManager.instance.passwordRegisterVerifyField.text)
        {
            //If the password does not match show a warning
            AccountUIManager.instance.warningRegisterText.text = "Password Does Not Match!";
        }
        else
        {
            var RegisterTask = auth.CreateUserWithEmailAndPasswordAsync(_email, _password);

            yield return new WaitUntil(predicate: () => RegisterTask.IsCompleted);

            if (RegisterTask.Exception != null)
            {
                //If there are errors handle them
                Debug.LogWarning(message: $"Failed to register task with {RegisterTask.Exception}");
                FirebaseException firebaseEx = RegisterTask.Exception.GetBaseException() as FirebaseException;
                AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

                string message = "Register Failed!";
                switch (errorCode)
                {
                    case AuthError.MissingEmail:
                        message = "Missing Email";
                        break;
                    case AuthError.MissingPassword:
                        message = "Missing Password";
                        break;
                    case AuthError.WeakPassword:
                        message = "Weak Password";
                        break;
                    case AuthError.EmailAlreadyInUse:
                        message = "Email Already In Use";
                        break;
                }
                AccountUIManager.instance.warningRegisterText.text = message;
            }
            else
            {
                User = RegisterTask.Result;

                if (User != null)
                {
                    UserProfile profile = new() { DisplayName = _username };
                    var ProfileTask = User.UpdateUserProfileAsync(profile);
                    yield return new WaitUntil(predicate: () => ProfileTask.IsCompleted);

                    if (ProfileTask.Exception != null)
                    {
                        //If there are errors handle them
                        Debug.LogWarning(message: $"Failed to register task with {ProfileTask.Exception}");
                        FirebaseException firebaseEx = ProfileTask.Exception.GetBaseException() as FirebaseException;
                        AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

                        string output = "Unknown Error, Please Try Again";
                        switch (errorCode)
                        {
                            case AuthError.Cancelled:
                                output = "Update User Cancelled";
                                break;
                            case AuthError.EmailAlreadyInUse:
                                output = "Email Already In Use";
                                break;
                        }

                        AccountUIManager.instance.warningRegisterText.text = output;
                    }
                    else
                    {
                        //AccountUIManager.instance.LoginScreen();
                        StartCoroutine(SendVerificationEmail());
                        AccountUIManager.instance.warningRegisterText.text = "";
                    }
                }
            }
        }
    }


    private IEnumerator UpdateUsernameAuth(string _username)
    {
        //Create a user profile and set the username
        UserProfile profile = new() { DisplayName = _username };

        //Call the Firebase auth update user profile function passing the profile with the username
        var ProfileTask = User.UpdateUserProfileAsync(profile);
        yield return new WaitUntil(predicate: () => ProfileTask.IsCompleted);

        if (ProfileTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {ProfileTask.Exception}");
        }
        else
        {
            //Auth username is now updated
        }
    }

    private IEnumerator UpdateUsernameDatabase(string _username)
    {
        //Call the Firebase auth update user profile function passing the profile with the username
        var DBTask = DBReference.Child("users").Child(User.UserId).Child("username").SetValueAsync(_username);
        yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

        if (DBTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
        }
        else
        {
            //Auth username is now updated
        }
    }

    public IEnumerator LoadUsernameData()
    {
        var DBTask = DBReference.Child("users").Child(User.UserId).GetValueAsync();        

        yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

        if (DBTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
        }
        else if (DBTask.Result.Value == null || DBTask.Result.Child("username") == null)
        {
            string random = "Guest " + Random.Range(0, 1000).ToString("000");
            AccountUIManager.instance.usernameInputField.text = random;
            StartCoroutine(UpdateUsernameAuth(random));
            StartCoroutine(UpdateUsernameDatabase(random));
            PhotonNetwork.NickName = random;
        }
        else
        {
            DataSnapshot snapshot = DBTask.Result; 

            if (snapshot.HasChild("username"))
            {
                AccountUIManager.instance.usernameInputField.text = snapshot.Child("username").Value.ToString();
                PhotonNetwork.NickName = snapshot.Child("username").Value.ToString();
                Debug.Log("Retrieved player username");
                AccountUIManager.instance.accountDetailsUsername.text = "Your username is: " + snapshot.Child("username").Value.ToString();
            }
            else
            {
                string random = "Guest " + Random.Range(0, 1000).ToString("000");
                AccountUIManager.instance.usernameInputField.text = random;
                StartCoroutine(UpdateUsernameAuth(random));
                StartCoroutine(UpdateUsernameDatabase(random));
                PhotonNetwork.NickName = random;
            }
        }
    }
    public IEnumerator LoadApplicationGenuinity()
    {
        var DBTask = DBReference.Child("users").Child(User.UserId).GetValueAsync();

        yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

        if (DBTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
        }
        else if (DBTask.Result.Value == null || DBTask.Result.Child("playTimes") == null)
        {
            AccountUIManager.instance.tutCanvas.SetActive(true);
            StartCoroutine(UpdateApplicationGenuinity(1));
        }
        else
        {
            DataSnapshot snapshot = DBTask.Result;

            if (snapshot.HasChild("playTimes"))
            {
                if (SceneManager.GetActiveScene().name == "Menu")
                {
                    if (int.Parse(snapshot.Child("playTimes").Value.ToString()) == 0)
                    {
                        AccountUIManager.instance.tutCanvas.SetActive(true);
                        StartCoroutine(UpdateApplicationGenuinity(1));

                    }
                    if (int.Parse(snapshot.Child("playTimes").Value.ToString()) == 1)
                    {
                        AccountUIManager.instance.tutCanvas.SetActive(false);
                    }
                }
            }
            else
            {
                AccountUIManager.instance.tutCanvas.SetActive(true);
                StartCoroutine(UpdateApplicationGenuinity(1));
            }
        }
    }

    public IEnumerator UpdateApplicationGenuinity(int freeforAllplayTime)
    {
        //Call the Firebase auth update user profile function passing the profile with the username
        var DBTask = DBReference.Child("users").Child(User.UserId).Child("playTimes").SetValueAsync(freeforAllplayTime);
        yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

        if (DBTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
        }
        else
        {
            StartCoroutine(LoadApplicationGenuinity());
        }
    }

    public IEnumerator UpdatePlayerColor(string _playerColorHexadecimal)
    {
        //Call the Firebase auth update user profile function passing the profile with the username
        var DBTask = DBReference.Child("users").Child(User.UserId).Child("playerColor").SetValueAsync(_playerColorHexadecimal);
        yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

        if (DBTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
        }
        else
        {
            //Auth username is now updated
        }
    }

    public IEnumerator UpdateKills(int newAmount)
    {
        //Call the Firebase auth update user profile function passing the profile with the username
        var DBTask = DBReference.Child("users").Child(User.UserId).Child("kills").SetValueAsync(newAmount);
        yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

        if (DBTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
        }
        else
        {
            //Auth username is now updated
            currentLVL = newAmount;
            StartCoroutine(LoadKills());
        }
    }

    public IEnumerator UpdateHats(int newAmount)
    {
        //Call the Firebase auth update user profile function passing the profile with the username
        var DBTask = DBReference.Child("users").Child(User.UserId).Child("hats").SetValueAsync(newAmount);
        yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

        if (DBTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
        }
        else
        {
            //Auth username is now updated
            AccessoriesManager.Singleton.equipedHat = newAmount;
            StartCoroutine(LoadHats());
        }
    }
    public IEnumerator UpdateEyewear(int newAmount)
    {
        //Call the Firebase auth update user profile function passing the profile with the username
        var DBTask = DBReference.Child("users").Child(User.UserId).Child("eyewear").SetValueAsync(newAmount);
        yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

        if (DBTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
        }
        else
        {
            //Auth username is now updated
            AccessoriesManager.Singleton.equipedEyewear = newAmount;
            StartCoroutine(LoadEyewear());
        }
    }
    public IEnumerator UpdateCapes(int newAmount)
    {
        //Call the Firebase auth update user profile function passing the profile with the username
        var DBTask = DBReference.Child("users").Child(User.UserId).Child("capes").SetValueAsync(newAmount);
        yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

        if (DBTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
        }
        else
        {
            //Auth username is now updated
            AccessoriesManager.Singleton.equipedCape = newAmount;
            StartCoroutine(LoadCapes());
        }
    }

    public IEnumerator UpdateSettings()
    {
        //Call the Firebase auth update user profile function passing the profile with the username
        
        if (settingsString != "")
        {
            var DBTask = DBReference.Child("users").Child(User.UserId).Child("settings").SetValueAsync(settingsString);

            yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

            if (DBTask.Exception != null)
            {
                Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
            }
            else
            {
                //Auth username is now updated
                StartCoroutine(nameof(LoadSettings));
            }
        }        
    }

    public void Btn_GetSettingsCharString()
    {
        List<string> arr = new();

        foreach (Toggle setting in AccountUIManager.instance.settings)
        {
            if (setting.isOn == true)
                arr.Add("t");
            else
                arr.Add("f");
        }

        settingsString = ListToText(arr);

        settingsArray = arr;

        StartCoroutine(nameof(UpdateSettings));
    }

    private string ListToText(List<string> list)
    {
        string settings = string.Join("", list);
        Debug.Log(settings);
        return settings;
    }


    public IEnumerator LoadSettings()
    {
        var DBTask = DBReference.Child("users").Child(User.UserId).GetValueAsync();

        yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

        if (DBTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
        }
        else if (DBTask.Result.Value == null || DBTask.Result.Child("settings") == null)
        {
            settingsArray.Clear();
            settingsArray.Add("t");
            settingsArray.Add("t");
            settingsArray.Add("t");
            settingsArray.Add("t");
            settingsArray.Add("f");
            GameObject.Find("Post-process Volume").SetActive(true);
            AccountUIManager.instance.accountCanvas.GetComponent<AudioSource>().enabled = true;
            AccountUIManager.instance.menuCanvas.GetComponent<AudioSource>().enabled = true;
            isMusicOn = true;
            alwaysSprint = false;
        }
        else
        {
            DataSnapshot snapshot = DBTask.Result;

            if (snapshot.HasChild("settings"))
            {
                ProcessSettings(snapshot.Child("settings").Value.ToString());

            }
            else
            {
                settingsArray.Clear();
                settingsArray.Add("t");
                settingsArray.Add("t");
                settingsArray.Add("t");
                settingsArray.Add("t");
                settingsArray.Add("f");
                GameObject.Find("Post-process Volume").SetActive(true);
                AccountUIManager.instance.accountCanvas.GetComponent<AudioSource>().enabled = true;
                AccountUIManager.instance.menuCanvas.GetComponent<AudioSource>().enabled = true;
                isMusicOn = true;
                alwaysSprint = true;
            }
        }
    }

    void ProcessSettings(string settings)
    {
        char[] myChars = settings.ToCharArray();

        if (myChars[0] == 't')
        {
            volume.SetActive(true);
            AccountUIManager.instance.settings[0].isOn = true;
        }
        if (myChars[0] == 'f')
        {
            volume.SetActive(false);
            AccountUIManager.instance.settings[0].isOn = false;
        }
        if (myChars[1] == 't')
        {
            if (SceneManager.GetActiveScene().name != "Menu")
                return;
            AccountUIManager.instance.accountCanvas.GetComponent<AudioSource>().enabled = true;
            AccountUIManager.instance.menuCanvas.GetComponent<AudioSource>().enabled = true;
            isMusicOn = true;
            AccountUIManager.instance.settings[1].isOn = true;

        }
        if (myChars[1] == 'f')
        {

            if (SceneManager.GetActiveScene().name != "Menu")
                return;
            AccountUIManager.instance.accountCanvas.GetComponent<AudioSource>().enabled = false;
            AccountUIManager.instance.menuCanvas.GetComponent<AudioSource>().enabled = false;
            isMusicOn = false;
            AccountUIManager.instance.settings[1].isOn = false;

        }
        if (myChars[2] == 't')
        {
            Screen.fullScreen = true;
            AccountUIManager.instance.settings[2].isOn = true;

        }
        if (myChars[2] == 'f')
        {
            Screen.fullScreen = false;
            AccountUIManager.instance.settings[2].isOn = false;

        }
        if (myChars[3] == 't')
        {
            crosshairs = true;
            AccountUIManager.instance.settings[3].isOn = true;

        }
        if (myChars[3] == 'f')
        {
            crosshairs = false;
            AccountUIManager.instance.settings[3].isOn = false;

        }
        if (myChars[4] == 't')
        {
            alwaysSprint = true;
            AccountUIManager.instance.settings[4].isOn = true;

        }
        if (myChars[4] == 'f')
        {
            alwaysSprint = false;
            AccountUIManager.instance.settings[4].isOn = false;

        }
    }


    public IEnumerator UpdateExperience(int newAmount)
    {
        //Call the Firebase auth update user profile function passing the profile with the username
        var DBTask = DBReference.Child("users").Child(User.UserId).Child("xp").SetValueAsync(newAmount);
        yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

        if (DBTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
        }
        else
        {
            //Auth username is now updated
            currentXP = newAmount;
            StartCoroutine(LoadExperience());
        }
    }

    public IEnumerator UpdateRemoveHats(bool newAmount)
    {
        //Call the Firebase auth update user profile function passing the profile with the username
        var DBTask = DBReference.Child("users").Child(User.UserId).Child("hatRemove").SetValueAsync(newAmount);
        yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

        if (DBTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
        }
        else
        {
            StartCoroutine(nameof(CheckForRemoveHat));
        }
    }
    public IEnumerator UpdateRemoveEyewear(bool newAmount)
    {
        //Call the Firebase auth update user profile function passing the profile with the username
        var DBTask = DBReference.Child("users").Child(User.UserId).Child("eyewearRemove").SetValueAsync(newAmount);
        yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

        if (DBTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
        }
        else
        {
            StartCoroutine(nameof(CheckForRemoveEyeWear));
        }
    }
    public IEnumerator UpdateRemoveCapes(bool newAmount)
    {
        //Call the Firebase auth update user profile function passing the profile with the username
        var DBTask = DBReference.Child("users").Child(User.UserId).Child("capeRemove").SetValueAsync(newAmount);
        yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

        if (DBTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
        }
        else
        {
            StartCoroutine(nameof(CheckForRemoveCape));
        }
    }

    public IEnumerator CheckForRemoveHat()
    {
        var DBTask = DBReference.Child("users").Child(User.UserId).GetValueAsync();

        yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

        if (DBTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
        }
        else if (DBTask.Result.Value == null || DBTask.Result.Child("hatRemove") == null)
        {
            AccessoriesManager.Singleton.removeHats = true;
            foreach (GameObject item in AccountUIManager.instance.hatChecks)
            {
                item.SetActive(false);
            }
        }
        else
        {
            DataSnapshot snapshot = DBTask.Result;

            if (snapshot.HasChild("hatRemove"))
            {
                AccessoriesManager.Singleton.removeHats = bool.Parse(snapshot.Child("hatRemove").Value.ToString());
            }
            else
            {
                AccessoriesManager.Singleton.removeHats = true;
                foreach (GameObject item in AccountUIManager.instance.hatChecks)
                {
                    item.SetActive(false);
                }
            }
        }
    }

    public IEnumerator CheckForRemoveEyeWear()
    {
        var DBTask = DBReference.Child("users").Child(User.UserId).GetValueAsync();

        yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

        if (DBTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
        }
        else if (DBTask.Result.Value == null || DBTask.Result.Child("eyewearRemove") == null)
        {
            AccessoriesManager.Singleton.removeEyewear = true;
            foreach (GameObject item in AccountUIManager.instance.eyeChecks)
            {
                item.SetActive(false);
            }
        }
        else
        {
            DataSnapshot snapshot = DBTask.Result;

            if (snapshot.HasChild("eyewearRemove"))
            {
                AccessoriesManager.Singleton.removeEyewear = bool.Parse(snapshot.Child("eyewearRemove").Value.ToString());
            }
            else
            {
                AccessoriesManager.Singleton.removeEyewear = true;
                foreach (GameObject item in AccountUIManager.instance.eyeChecks)
                {
                    item.SetActive(false);
                }
            }
        }
    }

    public IEnumerator CheckForRemoveCape()
    {
        var DBTask = DBReference.Child("users").Child(User.UserId).GetValueAsync();

        yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

        if (DBTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
        }
        else if (DBTask.Result.Value == null || DBTask.Result.Child("capeRemove") == null)
        {
            AccessoriesManager.Singleton.removeCapes = true;
            foreach (GameObject item in AccountUIManager.instance.capeChecks)
            {
                item.SetActive(false);
            }
        }
        else
        {
            DataSnapshot snapshot = DBTask.Result;

            if (snapshot.HasChild("capeRemove"))
            {
                AccessoriesManager.Singleton.removeCapes = bool.Parse(snapshot.Child("capeRemove").Value.ToString());
            }
            else
            {
                AccessoriesManager.Singleton.removeCapes = true;
                foreach (GameObject item in AccountUIManager.instance.capeChecks)
                {
                    item.SetActive(false);
                }
            }
        }
    }

    public IEnumerator LoadKills()
    {
        var DBTask = DBReference.Child("users").Child(User.UserId).GetValueAsync();

        yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

        if (DBTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
        }
        else if (DBTask.Result.Value == null || DBTask.Result.Child("kills") == null)
        {
            LevelUpManager.Singleton.currentLevel = 0;
        }
        else
        {
            DataSnapshot snapshot = DBTask.Result;

            if (snapshot.HasChild("kills"))
            {
                Debug.Log("Looks like kills is not null");
                LevelUpManager.Singleton.currentLevel = int.Parse(snapshot.Child("kills").Value.ToString());
                currentLVL = int.Parse(snapshot.Child("kills").Value.ToString());

                if (SceneManager.GetActiveScene().name == "Menu")
                {
                    int level = int.Parse(snapshot.Child("kills").Value.ToString());
                    AccountUIManager.instance.levelText.text = "You're at level " + level + "!";
                }

                _customProps["playerLevel"] = int.Parse(snapshot.Child("kills").Value.ToString());
                PhotonNetwork.LocalPlayer.CustomProperties = _customProps;

                Debug.Log("player " + PhotonNetwork.LocalPlayer);
            }
            else
            {
                LevelUpManager.Singleton.currentLevel = 0;

                currentLVL = 0;



                _customProps["playerLevel"] = 0;
                PhotonNetwork.LocalPlayer.CustomProperties = _customProps;

                if (SceneManager.GetActiveScene().name == "Menu")
                {
                    AccountUIManager.instance.levelText.text = "You're at level 0!";
                }
            }


            //Do all the stuff with the main menu level up bar (hint: check if the current scene is 0, if so, do the mathf.epsilon pattern
        }
    }

    public IEnumerator LoadHats()
    {
        var DBTask = DBReference.Child("users").Child(User.UserId).GetValueAsync();

        yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

        if (DBTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
        }
        else if (DBTask.Result.Value == null || DBTask.Result.Child("hats") == null)
        {
            AccessoriesManager.Singleton.equipedHat = 0;
            foreach (GameObject item in AccountUIManager.instance.hatChecks)
            {
                item.SetActive(false);
            }
        }
        else
        {
            DataSnapshot snapshot = DBTask.Result;

            if (snapshot.HasChild("hats"))
            {
                AccessoriesManager.Singleton.equipedHat = int.Parse(snapshot.Child("hats").Value.ToString());
                Debug.Log("The Firebase pulled hat value " + int.Parse(snapshot.Child("hats").Value.ToString()));

                for (int i = 0; i < AccountUIManager.instance.hatChecks.Length; i++)
                {
                    if (!AccessoriesManager.Singleton.removeHats)
                    {
                        if (i != int.Parse(snapshot.Child("hats").Value.ToString()))
                        {
                            AccountUIManager.instance.hatChecks[i].SetActive(false);
                            Debug.Log(AccountUIManager.instance.hatChecks[i]);
                        }
                        else
                        {
                            AccountUIManager.instance.hatChecks[i].SetActive(true);
                        }
                    }
                }
            }
            else
            {
                AccessoriesManager.Singleton.equipedHat = 0;
                foreach (GameObject item in AccountUIManager.instance.hatChecks)
                {
                    item.SetActive(false);
                }
            }
        }
    }

    public IEnumerator LoadEyewear()
    {
        var DBTask = DBReference.Child("users").Child(User.UserId).GetValueAsync();

        yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

        if (DBTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
        }
        else if (DBTask.Result.Value == null || DBTask.Result.Child("eyewear") == null)
        {
            AccessoriesManager.Singleton.equipedEyewear = 0;
            foreach (GameObject item in AccountUIManager.instance.eyeChecks)
            {
                item.SetActive(false);
            }
        }
        else
        {
            DataSnapshot snapshot = DBTask.Result;

            if (snapshot.HasChild("eyewear"))
            {
                AccessoriesManager.Singleton.equipedEyewear = int.Parse(snapshot.Child("eyewear").Value.ToString());

                for (int i = 0; i < AccountUIManager.instance.eyeChecks.Length; i++)
                {
                    if (!AccessoriesManager.Singleton.removeEyewear)
                    {
                        if (i != int.Parse(snapshot.Child("eyewear").Value.ToString()))
                        {
                            AccountUIManager.instance.eyeChecks[i].SetActive(false);
                        }
                        else
                        {
                            AccountUIManager.instance.eyeChecks[i].SetActive(true);
                        }
                    }
                }
            }
            else
            {
                AccessoriesManager.Singleton.equipedEyewear = 0;
                foreach (GameObject item in AccountUIManager.instance.eyeChecks)
                {
                    item.SetActive(false);
                }
            }
        }
    }
    public IEnumerator LoadCapes()
    {
        var DBTask = DBReference.Child("users").Child(User.UserId).GetValueAsync();

        yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

        if (DBTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
        }
        else if (DBTask.Result.Value == null || DBTask.Result.Child("capes") == null)
        {
            AccessoriesManager.Singleton.equipedCape = 0;
            foreach (GameObject item in AccountUIManager.instance.capeChecks)
            {
                item.SetActive(false);
            }
        }
        else
        {
            DataSnapshot snapshot = DBTask.Result;

            if (snapshot.HasChild("capes"))
            {
                AccessoriesManager.Singleton.equipedCape = int.Parse(snapshot.Child("capes").Value.ToString());

                for (int i = 0; i < AccountUIManager.instance.capeChecks.Length; i++)
                {
                    if (!AccessoriesManager.Singleton.removeCapes)
                    {
                        if (i != int.Parse(snapshot.Child("capes").Value.ToString()))
                        {
                            AccountUIManager.instance.capeChecks[i].SetActive(false);
                        }
                        else
                        {
                            AccountUIManager.instance.capeChecks[i].SetActive(true);
                        }
                    }
                }
            }
            else
            {
                AccessoriesManager.Singleton.equipedCape = 0;
                foreach (GameObject item in AccountUIManager.instance.capeChecks)
                {
                    item.SetActive(false);
                }
            }
        }
    }

    public IEnumerator LoadExperience()
    {
        var DBTask = DBReference.Child("users").Child(User.UserId).GetValueAsync();

        yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

        if (DBTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
        }
        else if (DBTask.Result.Value == null || DBTask.Result.Child("xp") == null)
        {
            LevelUpManager.Singleton.currentExperience = 0;
        }
        else
        {
            DataSnapshot snapshot = DBTask.Result;

            

            if (snapshot.HasChild("xp"))
            {
                LevelUpManager.Singleton.currentExperience = int.Parse(snapshot.Child("xp").Value.ToString());

                currentXP = int.Parse(snapshot.Child("xp").Value.ToString());

                if (SceneManager.GetActiveScene().name == "Menu")
                {
                    int level = int.Parse(snapshot.Child("xp").Value.ToString());
                    AccountUIManager.instance.levelBar.value = level * 1.0f / 20;
                    AccountUIManager.instance.XPText.text = 20 - level + " more XP until you level up!"; 
                    Debug.Log(AccountUIManager.instance.levelBar);
                    Debug.Log(level * 1.0f / 20);
                }                
            }
            else
            {
                LevelUpManager.Singleton.currentExperience = 0;

                currentXP = 0;

                if (SceneManager.GetActiveScene().name == "Menu")
                {
                    AccountUIManager.instance.levelBar.value = 0;
                }
            }            
        }
    }


    public IEnumerator LoadPlayerColorDataCustomizeBeanModel(GameObject beanModel, FlexibleColorPicker fcp, Material healthyMat)
    {
        var DBTask = DBReference.Child("users").Child(User.UserId).GetValueAsync();

        yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

        if (DBTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
        }
        else if (DBTask.Result.Value == null || DBTask.Result.Child("playerColor") == null)
        {
            fcp.SetColor(Color.red);
            healthyMat.SetColor(("_MaterialColor"), Color.red);
            StartCoroutine(UpdatePlayerColor(ColorUtility.ToHtmlStringRGB(healthyMat.GetColor("_MaterialColor"))));
        }
        else
        {
            DataSnapshot snapshot = DBTask.Result;

            Debug.Log("User's player color is..." + snapshot.Child("playerColor").Value.ToString());

            if (ColorUtility.TryParseHtmlString("#" + snapshot.Child("playerColor").Value.ToString(), out Color playerColor))
            {
                beanModel.transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).transform.gameObject.GetComponent<MeshRenderer>().material.color = playerColor;
                healthyMat.SetColor(("_MaterialColor"), playerColor);
            }
            
            fcp.TypeHex(snapshot.Child("playerColor").Value.ToString());
        }
    }

    public IEnumerator LoadPlayerColorDataMainMenuBeanModel(GameObject beanModel, FlexibleColorPicker fcp, Material healthyMat)
    {
        var DBTask = DBReference.Child("users").Child(User.UserId).GetValueAsync();

        yield return new WaitUntil(predicate: () => DBTask.IsCompleted);

        if (DBTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {DBTask.Exception}");
        }
        else if (DBTask.Result.Value == null || DBTask.Result.Child("playerColor") == null)
        {
            fcp.SetColor(Color.red);
            healthyMat.SetColor(("_MaterialColor"), Color.red);
            StartCoroutine(UpdatePlayerColor("FF0000"));
        }
        else
        {
            DataSnapshot snapshot = DBTask.Result;

            Debug.Log("User's player color is..." + snapshot.Child("playerColor").Value.ToString());

            if (ColorUtility.TryParseHtmlString("#" + snapshot.Child("playerColor").Value.ToString(), out Color playerColor))
            {
                beanModel.transform.GetChild(0).transform.GetChild(0).transform.gameObject.GetComponent<MeshRenderer>().material.color = playerColor;
                healthyMat.SetColor(("_MaterialColor"), playerColor);
            }
            fcp.TypeHex(snapshot.Child("playerColor").Value.ToString());

            playerColorValue = snapshot.Child("playerColor").Value.ToString();            
        }
    }


    private IEnumerator SendVerificationEmail()
    {
        if (User != null)
        {
            var emailTask = User.SendEmailVerificationAsync();

            yield return new WaitUntil(predicate: () => emailTask.IsCompleted);

            if (emailTask.Exception != null)
            {
                FirebaseException firebaseException = (FirebaseException)emailTask.Exception.GetBaseException();
                AuthError error = (AuthError)firebaseException.ErrorCode;

                string output = "Unknown Error, Try Again!";

                switch (error)
                {
                    case AuthError.Cancelled:
                        output = "Verification Task was Cancelled";
                        break;
                    case AuthError.InvalidRecipientEmail:
                        output = "Invalid Email";
                        break;
                    case AuthError.TooManyRequests:
                        output = "Too Many Requests";
                        break;                    
                }                

                AccountUIManager.instance.AwaitVerification(false, User.Email, output);
            }
            else
            {
                AccountUIManager.instance.AwaitVerification(true, User.Email, null);
                Debug.Log("Email Sent Successfuly");
            }
        }
    }

    public void SendPasswordResetEmailButton()
    {
        StartCoroutine(SendPasswordResetEmail());
    }

    private IEnumerator SendPasswordResetEmail()
    {
        User = auth.CurrentUser;
        Debug.Log("Reset Password Has been Sent");
        var emailTask = auth.SendPasswordResetEmailAsync(AccountUIManager.instance.resetPasswordEmailText.text);

        yield return new WaitUntil(predicate: () => emailTask.IsCompleted);

        if (emailTask.Exception != null)
        {
            FirebaseException firebaseException = (FirebaseException)emailTask.Exception.GetBaseException();
            AuthError error = (AuthError)firebaseException.ErrorCode;

            string output = "Unknown Error, Try Again!";

            switch (error)
            {
                case AuthError.Cancelled:
                    output = "Verification Task was Cancelled";
                    break;
                case AuthError.InvalidRecipientEmail:
                    output = "Invalid Email";
                    break;
                case AuthError.TooManyRequests:
                    output = "Too Many Requests";
                    break;
            }

            //TODO: CALL FUNCTION IN ACCOUNTUIMANAGER THAT WILL SET THE TEXT CHILD WITH ERROR OUTPUT
            AccountUIManager.instance.AwaitReset(false, AccountUIManager.instance.resetPasswordEmailText.text, output);
            Debug.Log("Email Not Sent Successfuly");
        }
        else
        {
            //TODO: CALL FUNCTION IN ACCOUNTUIMANAGER THAT WILL SET THE TEXT CHILD WITH SUCESS OUTPUT
            AccountUIManager.instance.AwaitReset(true, AccountUIManager.instance.resetPasswordEmailText.text, null);
            Debug.Log("Email Sent Successfuly");
        }
    }

    public void CheckUserCredentialsButton()
    {
        StartCoroutine(CheckUserCredentials(AccountUIManager.instance.resetEmailVerifyEmail, AccountUIManager.instance.resetEmailVerifyPassword, AccountUIManager.instance.resetEmailErrorText));
    }


    public IEnumerator CheckUserCredentials(TMP_InputField _email, TMP_InputField _password, TMP_Text errorText)
    {
        User = auth.CurrentUser;
        Credential credential = EmailAuthProvider.GetCredential(_email.text, _password.text);

        var reverifyTask = User.ReauthenticateAsync(credential);

        yield return new WaitUntil(predicate: () => reverifyTask.IsCompleted);

        if (reverifyTask.Exception != null)
        {
            FirebaseException firebaseException = (FirebaseException)reverifyTask.Exception.GetBaseException();
            AuthError error = (AuthError)firebaseException.ErrorCode;

            string message = "Verification Failed!";
            switch (error)
            {
                case AuthError.MissingEmail:
                    message = "Missing Email";
                    break;
                case AuthError.MissingPassword:
                    message = "Missing Password";
                    break;
                case AuthError.InvalidEmail:
                    message = "Invalid Email";
                    break;
            }

            errorText.text = message;
        }
        else
        {
            //TODO: MAKE EMAIL RESET SCREEN AND TAKE THE PLAYER THERE
            AccountUIManager.instance.reauthenticateUserForEmailResetMenu.SetActive(false);
            AccountUIManager.instance.resetEmailNewEmailMenu.SetActive(true);
            _email.text = "";
            _password.text = "";
            errorText.text = "";
        }
    }

    public void ResetEmailButton()
    {
        StartCoroutine(ResetEmail(AccountUIManager.instance.resetEmailNewEmail, AccountUIManager.instance.resetEmailNewEmailErrorText));
    }

    public IEnumerator ResetEmail(TMP_InputField _newEmail, TMP_Text errorText)
    {
        User = auth.CurrentUser; 

        var changeEmailTask = User.UpdateEmailAsync(_newEmail.text);

        yield return new WaitUntil(predicate: () => changeEmailTask.IsCompleted);

        if (changeEmailTask.Exception != null)
        {
            FirebaseException firebaseException = (FirebaseException)changeEmailTask.Exception.GetBaseException();
            AuthError error = (AuthError)firebaseException.ErrorCode;

            string message = "Verification Failed!";
            switch (error)
            {
                case AuthError.MissingEmail:
                    message = "Missing Email";
                    break;                
                case AuthError.InvalidEmail:
                    message = "Invalid Email";
                    break;
                case AuthError.EmailAlreadyInUse:
                    message = "Email Already in Use!";
                    break;
            }
            errorText.color = Color.red;
            errorText.text = message;
        }
        else
        {
            errorText.color = Color.green;
            errorText.text = "Reset Email Sucessfully!";
            yield return new WaitForSeconds(2);
            _newEmail.text = "";

            errorText.text = "";
            errorText.color = Color.red;
            AccountUIManager.instance.resetEmailNewEmailMenu.SetActive(false);
            AccountUIManager.instance.resetEmailNewEmailMenu.SetActive(false);
            AccountUIManager.instance.titleMenu.SetActive(true);
        }
    }
}
