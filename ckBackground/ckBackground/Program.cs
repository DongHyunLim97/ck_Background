using System;
using System.IO;
using System.Diagnostics;
using IWshRuntimeLibrary;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;



class Program
{
    // 바탕화면 배경을 설정하는 Windows API 호출
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

    const int SPI_SETDESKWALLPAPER = 20;
    const int SPIF_UPDATEINIFILE = 0x01;
    const int SPIF_SENDCHANGE = 0x02;

    const string ProgVersion = "20250731";

    enum spaceIdx
    {
        conv, convLab, game
    };

    const spaceIdx idx = spaceIdx.game;

    //이미지링크 
    //융콘
    const string convImageUrl = "https://raw.githubusercontent.com/DongHyunLim97/ck_Background/refs/heads/main/BackGround/Conv/conv.png";
    //웅콘랩실
     const string convLabImageUrl = "https://raw.githubusercontent.com/DongHyunLim97/ck_Background/refs/heads/main/BackGround/ConvLab/convLab.png";
    //게임
     const string gameImageUrl = "https://raw.githubusercontent.com/DongHyunLim97/ck_Background/refs/heads/main/BackGround/Game/game.png";

    //실행파일링크
    //융콘
    const string convExeUrl = "https://github.com/DongHyunLim97/ck_Background/raw/refs/heads/main/BackGround/Conv/ckBackground.exe";
    //융콘랩
    const string convlabExeUrl = "https://github.com/DongHyunLim97/ck_Background/raw/refs/heads/main/BackGround/ConvLab/ckBackground.exe";
    //게임
    const string gameExeUrl = "https://github.com/DongHyunLim97/ck_Background/raw/refs/heads/main/BackGround/Game/ckBackground.exe";

    //버전링크
    const string versionUrl = "https://raw.githubusercontent.com/DongHyunLim97/ck_Background/main/BackGround/version.txt";

    [RequiresAssemblyFiles("Calls System.Reflection.Assembly.Location")]
    static async Task Main(string[] args)
    {
        string imgTemp = null;
        string exeTemp = null;
        switch (idx)
        {
            case spaceIdx.conv:
                imgTemp = convImageUrl;
                exeTemp = convExeUrl;
                break;
            case spaceIdx.convLab:
                imgTemp = convLabImageUrl;
                exeTemp = convlabExeUrl;
                break;
            case spaceIdx.game:
                imgTemp = gameImageUrl;
                exeTemp = gameExeUrl;
                break;
            default:
                break;
        }
        Console.WriteLine("청강대 행정실에서 관리하는 프로그램입니다.");
        Console.WriteLine("종료하지 말아주세요.");
        Console.WriteLine("인터넷 연결을 기다리고 있습니다...");

        await WaitForInternetConnectionAsync();

        await UpdateCheck(exeTemp);

        string downloadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "downloaded_image.png");

        // 이미지를 다운로드
        await DownloadImageAsync(imgTemp, downloadPath);

        // 이미지를 바탕화면으로 설정
        SetWallpaper(downloadPath);

        Console.WriteLine("이미지가 다운로드되어 바탕화면이 설정되었습니다.");


        // 현재 실행 중인 프로그램의 경로 가져오기
        string currentExePath = Process.GetCurrentProcess().MainModule.FileName;

        // 시작 프로그램 폴더 경로 가져오기
        string startupFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        string shortcutPath = Path.Combine(startupFolderPath, Path.GetFileNameWithoutExtension(currentExePath) + ".lnk");


        // 바로가기 생성
        CreateShortcut(shortcutPath, currentExePath);

        Console.WriteLine("3초 후 프로그램을 자동으로 종료합니다.");
        await Task.Delay(3000);
        // 프로그램 종료
        Application.ExitThread();
        Environment.Exit(0);
    }
    // 인터넷 연결을 기다리는 함수
    static async Task WaitForInternetConnectionAsync()
    {
        while (true)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync("https://www.google.com");
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("인터넷이 연결되었습니다.");
                        break; // 인터넷이 연결되면 루프 탈출
                    }
                }
            }
            catch
            {
                // 인터넷 연결이 없으면 계속 기다림
                Console.WriteLine("인터넷 연결이 없습니다. 다시 시도합니다...");
            }

            await Task.Delay(10000); // 10초마다 확인
        }
    }
    // 버전체크
    static async Task UpdateCheck(string exeTemp)
    {
        using (HttpClient client = new HttpClient())
        {
            try
            {
                //실행파일의 위치를 확인
                // 현재 실행 중인 .exe 파일의 전체 경로
                string exePath = AppContext.BaseDirectory;
                // 실행 파일이 위치한 디렉토리 경로
                string exeDirectory = Path.GetDirectoryName(exePath);
                // "내 문서" 폴더 경로
                string myDocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                if (exeDirectory != myDocumentsPath)
                    throw new Exception("파일 위치 불일치.");


                string content = await client.GetStringAsync(versionUrl);

                if (ProgVersion != content) //버전 불일치
                    throw new Exception("새로운 버전이 있습니다.");
                
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("파일을 가져오는 데 실패했습니다:");
                Console.WriteLine(e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                await DownloadNewVersion(exeTemp);
            }
        }

    }
    // 신규 실행파일 다운로드
    static async Task DownloadNewVersion(string exeTemp)
    {
        Console.WriteLine("새 버전 다운로드");

        //로컬에 저장할 경로
        string localFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ckBackground.exe");

        Console.WriteLine("파일 다운로드 시작...");

        using (HttpClient client = new HttpClient())
        {
            try
            {
                byte[] exeData = await client.GetByteArrayAsync(exeTemp);
                await System.IO.File.WriteAllBytesAsync(localFilePath, exeData);

                Console.WriteLine("파일 다운로드 완료.");

                // 3. 파일 실행
                Console.WriteLine("파일 실행 중...");
                Process.Start(new ProcessStartInfo
                {
                    FileName = localFilePath,
                    UseShellExecute = true  // 관리자 권한이 필요한 경우 false로 바꾸고 Verb = "runas" 설정 가능
                });

                Application.ExitThread();
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"오류 발생: {ex.Message}");
            }
        }

    }

    // 이미지를 다운로드하는 함수
    static async Task DownloadImageAsync(string url, string path)
    {
        using (HttpClient client = new HttpClient())
        {
            byte[] imageBytes = await client.GetByteArrayAsync(url);
            await System.IO.File.WriteAllBytesAsync(path, imageBytes);
        }
    }

    // 다운로드한 이미지를 바탕화면 배경으로 설정하는 함수
    static void SetWallpaper(string imagePath)
    {
        SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, imagePath, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
    }

    // 바로가기를 생성하는 함수
    static void CreateShortcut(string shortcutPath, string targetPath)
    {
        if (System.IO.File.Exists(shortcutPath))
        {
            //Console.WriteLine("바로가기가 이미 존재합니다.");
            return;
        }

        WshShell shell = new WshShell();
        IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = targetPath; // 실행할 파일 경로
        shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath); // 실행 파일의 디렉토리
        shortcut.Description = "자동 실행 프로그램";
        shortcut.Save(); // 바로가기 저장

        Console.WriteLine($"바로가기 생성 완료");
    }

}