using System;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

public class TitleSceneController : MonoBehaviour
{
    private VisualElement _root;
    private Button _btnNewGame;
    private Button _btnContinue;

    // 리더님이 지정하신 세이브 데이터 경로
    private string _saveFolderPath;

    private async void Start()
    {
#if UNITY_EDITOR
        _saveFolderPath = Path.Combine(Application.dataPath, "Data", "Save");
#else
        _saveFolderPath = Path.Combine(Application.persistentDataPath, "Save");
#endif

        // 프리로드가 완료될 때까지 버튼 상호작용을 차단하여 데드락 및 예외 방지
        if (_btnNewGame != null) _btnNewGame.SetEnabled(false);
        if (_btnContinue != null) _btnContinue.SetEnabled(false);

        // 라벨을 통한 전체 프리로드
        await DBSORegistry.PreloadByLabelAsync("DBSO");
        
        // 라벨이 누락되어 있을 경우를 대비해 Town 씬과 캐릭터 생성에 필수적인 DB들을 명시적으로 비동기 로드
        await DBSORegistry.PreloadAsync(
            "SkillDataBase", 
            "ClassDataBase", 
            "DefaultEquipDataBase", 
            "MonsterDataBase",
            "JournalDataBase",
            "BuildingDataBase",
            "AccessoryDataBase",
            "ReactionDefinitionDataBase",
            "StatModifierDataBase"
        );

        // 프리로드 완료 후 '새 게임' 버튼 활성화 및 세이브 존재 여부 체크
        if (_btnNewGame != null) _btnNewGame.SetEnabled(true);
        CheckSaveDataExistence();
    }

    private void OnEnable()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;

        _btnNewGame = _root.Q<Button>("btn-new-game");
        _btnContinue = _root.Q<Button>("btn-continue");

        if (_btnNewGame != null) _btnNewGame.clicked += OnNewGameClicked;
        if (_btnContinue != null) _btnContinue.clicked += OnContinueClicked;
    }

    private void CheckSaveDataExistence()
    {
        if (_btnContinue == null) return;

        // 세이브 폴더가 존재하고, 그 내부에 파일(.json 등)이 하나라도 있는지 체크
        if (Directory.Exists(_saveFolderPath) && Directory.GetFiles(_saveFolderPath).Length > 0)
        {
            _btnContinue.SetEnabled(true);
        }
        else
        {
            // 세이브 파일이 아예 없다면 이어하기를 못 누르게 막음
            _btnContinue.SetEnabled(false); 
            Debug.Log("[타이틀] 기존 세이브 데이터가 없어 '이어하기' 버튼이 비활성화됩니다.");
        }
    }

    private void OnNewGameClicked()
    {
        Debug.Log("<color=yellow>[새로하기]</color> 세이브 데이터를 전체 삭제하고 게임을 시작합니다.");

        // 1. 세이브 데이터 파일 전체 삭제
        DeleteAllSaveData();

        // 2. 타운 씬으로 전환 (씬 이름은 프로젝트에 맞게 "TownScene" 등으로 변경하세요)
        LoadGameScene();
    }

    private void OnContinueClicked()
    {
        Debug.Log("<color=green>[이어하기]</color> 기존 세이브 데이터를 유지한 채 게임을 이어합니다.");
        
        // 세이브 데이터를 건드리지 않고 그대로 타운 씬으로 진입
        LoadGameScene();
    }

    private void DeleteAllSaveData()
    {
        if (Directory.Exists(_saveFolderPath))
        {
            try
            {
                // 폴더 내의 모든 파일 삭제
                string[] files = Directory.GetFiles(_saveFolderPath);
                foreach (string file in files)
                {
                    File.Delete(file);
                }

                // 폴더 내의 하위 폴더까지 있다면 싹 청소
                string[] dirs = Directory.GetDirectories(_saveFolderPath);
                foreach (string dir in dirs)
                {
                    Directory.Delete(dir, true);
                }

                Debug.Log($"<color=red>[삭제 완료]</color> 경로 내부의 모든 세이브 파일이 완전히 제거되었습니다: {_saveFolderPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[세이브 삭제 실패] 파일 청소 중 오류 발생: {e.Message}");
            }
        }
        else
        {
            Debug.Log("[세이브 삭제] 삭제할 세이브 폴더가 존재하지 않습니다.");
        }
    }

    private void LoadGameScene()
    {
        SceneLoader.Load("Town"); 
    }
}