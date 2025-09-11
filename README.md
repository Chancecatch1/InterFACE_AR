***Installation***  
**Step 0: What you need**  
•  HoloLens 2 with internet access  
•  A Windows PC with a USB-C cable  
•  The app package, unzipped on your PC

**Step 1: Prepare the package**  
•  The folder should contain the app installer (the 'brown box' icon) and related files  
*(Depending on the device, the icon may not appear as a brown box, but select the file with the extension '**.appx'**)*  

**Step 2: Copy files to the device**  
•  Connect HoloLens 2 to the PC via USB-C.  
•  On your PC, open File Explorer and find 'HOLOLENS' in the left sidebar (with the HoloLens turned on).  
•  Drag and drop the unzipped 'app package' folder into the 'Downloads' folder.  

 **Step 3: Enable Developer Mode (if not already enabled)**  
•  On HoloLens 2: Settings -> Update & Security -> For developers -> turn on Developer Mode.

**Step 4: Install on the device**  
•  On HoloLens 2: open All apps -> File Explorer.  
•  The File Explorer defaults to 'Recent files'. Switch to 'This Device' to find your app package.  
•  Navigate to the Downloads -> app package folder ('InterFACE_AR_1.1.0.0_ARM64_Test') you copied.  
•  Open the folder and select the installer (the 'brown box' icon).  
•  If prompted, enter the device passcode, then tap Install (bottom right).  
•  Wait for installation to complete. The app appears in All apps, and the application may start right away.

**Step 5: Launch and clean up**  
•  Find and launch 'InterFACE_AR' from All apps.  
•  To remove old builds, press and hold an old app's icon and select Remove/Delete.

***User Guide***

1. **Starting the app**

•  Launch 'InterFACE_AR' from All apps.  
•  Select your role: Doctor or Nurse.

2. **Handmenu (visible next to your wrist)**  

•  **Pen Mode (<> / Thumbs-up)**: Toggle object manipulation. Use it to move/rotate/resize virtual windows. Toggle again to lock windows (Thumbs-up icon).  
•  **Sessions**: Show/hide the session list.  
•  **Maximize Medication**: Show the medication panel.  
•  **Minimize Medication**: Hide the medication panel.  
•  **Save Position (Pin)**: Save the current layout (window positions/rotations/scales).  
•  **Load Position (Star)**: Restore your most recently saved layout.  
•  **Reset Position (Arrow)**: Return all windows to the default layout.

3. **Working with holograms**

•  Refresh live sessions and select the currently active session.  
•  In Pen Mode, arrange the virtual windows as needed.  
•  To move a window, grab the bottom bar and drag it.  
•  To rotate or resize, tap/click the bottom bar to toggle manipulation handles; use them to rotate/resize. Tap again to hide the handles.

---

## Project Overview
- **Goal**: Provide CPR training/simulation in a Mixed Reality environment (InterFACE AR).  
- **Engine**: Unity 2022.3.15f1.  
- **Frameworks**: MRTK3, OpenXR, TextMesh Pro.  
- **Networking**: Socket.IO (client `SocketIOUnity`) and SSE (Server-Sent Events).  
- **JSON**: `Assets/SimpleJSON.cs` for parsing; resources under `Assets/Resources/**`.

## Build & Deploy (Editor/PC and HoloLens)
1) Open the project in Unity 2022.3.15f1.  
2) Ensure OpenXR and MRTK3 packages are installed (see `Packages/manifest.json`).  
3) File -> Build Settings -> UWP (HoloLens) or Win64 (PC).  
4) Player Settings:  
   - XR Plug-in Management: OpenXR enabled.  
   - Capabilities: InternetClient, SpatialPerception, Microphone (if voice used).  
5) Build: Create an AppX package for HoloLens (Master/ARM64).  
6) Deploy: Copy to device and install as described above.

## Usage & Controls
- Use hand rays or direct hand interaction per MRTK3.  
- Handmenu provides quick access to layout/session/medication controls.  
- Positioning: Use Pen Mode for placement; Save/Load to reuse preferred layout.  
- Sessions: Connect to an active session before starting training.

## Networking & Data
- Socket.IO connection is managed by `Assets/SocketEventManager.cs` (Newtonsoft serializer).  
- SSE reader: `Assets/EventSourceReader.cs` streams server events; UI updates run on main thread via `Assets/UnityMainThread.cs`.  
- Medication data: loaded by `Assets/medication_json.cs` and `Assets/MedicationEventManager.cs` from `Assets/Resources/**`.  
- Language text: handled by `Assets/language_json.cs` using `Resources/en.json`, `fr.json`. Keep keys in sync.

## Repository Structure (key paths)
- `Assets/**/*.cs`: core scripts.  
- `Assets/Resources/**`: JSON and other resources.  
- `Assets/UI_Prefab/**`: UI prefabs.  
- `Assets/Scenes/**`: Unity scenes.  
- `ProjectSettings/**`: project configuration.  
- `Packages/manifest.json`: package versions.

## Troubleshooting
- If UI does not update: ensure updates occur on main thread using `UnityMainThread.wkr.AddJob(...)`.  
- If images/resources not found: verify `Resources.Load<...>(path)` paths and file names.  
- If OpenXR errors: confirm OpenXR is enabled and profiles set in Player Settings.  
- If networking fails: check device network, server URL, and Socket.IO connection in `SocketEventManager`.  
- If localization missing: ensure `en.json`/`fr.json` contain matching keys.

---

## Notes
- This README intentionally contains no embedded images for maximum compatibility on GitHub.  
- For screenshots, store them under `Assets/Documentation/` and reference with relative paths, or host them externally.


