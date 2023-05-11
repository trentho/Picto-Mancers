using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
public Dropdown mapDropdown;
    public List<string> mapList;

    private int selectedMapIndex;

    private void Start()
    {
        mapDropdown.ClearOptions();
        mapDropdown.AddOptions(mapList);
        mapDropdown.onValueChanged.AddListener(UpdateSelectedMap);
        selectedMapIndex = 0;
    }

    public void UpdateSelectedMap(int mapIndex)
    {
        selectedMapIndex = mapIndex;
    }

    public void StartGame()
    {
        string selectedMapName = mapList[selectedMapIndex];
        SceneManager.LoadScene(selectedMapName);
    }


    public void ExitButton()
    {
        Application.Quit();
    }


    
}
