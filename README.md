# InterFACE AR

A mixed reality CPR training and simulation app built with Unity

## Groq api setup

### Recommended: keys.json at runtime

The app reads your Groq API key from `Application.persistentDataPath/keys.json`.

- keys.json format:
```json
{
  "groqApiKey": "gsk_your_real_key_here"
}
```

- HoloLens (Device Portal)
  - Build and install the app once so its data folder exists.
  - Open `https://<HoloLens-IP>/` and sign in.
  - File Explorer → LocalAppData → your app’s package family → `LocalState`
  - Upload `keys.json`
  - Restart the app

- Editor/PC
  - Place `keys.json` in `Application.persistentDataPath` on your machine.
  - You can print the exact path via:
    - `Debug.Log(Application.persistentDataPath);`