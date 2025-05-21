using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonManager : MonoBehaviour
{

    public void onExitBuuton()
    {
        Application.Quit();
    }
   
    // 임시/저장 볼륨값
    private float tempBGMVolume;
    private float savedBGMVolume;

    [Header("AudioSource")]
    public AudioSource bgmSource;

    [Header("사운드1 관련")]
    public Slider slider1;
    public Image soundWaveImg1;
    public List<Sprite> soundWaves1;
 
    void Start()
    {
        savedBGMVolume = PlayerPrefs.GetFloat("BGMVolume", 0.5f);

        tempBGMVolume = savedBGMVolume;


        slider1.value = tempBGMVolume;
        SetBGMVolume(tempBGMVolume);

        slider1.onValueChanged.AddListener(OnBGMVolumeChanged);

        UpdateSoundWave1();

        Debug.Log("BGMVolume: " + PlayerPrefs.GetFloat("BGMVolume", 0.5f));
    }

    public void OnBGMVolumeChanged(float value)
    {
        tempBGMVolume = value;
        SetBGMVolume(tempBGMVolume); // 임시 적용(미리듣기)
        UpdateSoundWave1();
    }


    public void SetBGMVolume(float value)
    {
        bgmSource.volume = value;
    }



    // 저장 버튼: 임시값을 진짜로 저장
    public void ClickSave()
    {
        PlayerPrefs.SetFloat("BGMVolume", tempBGMVolume);
    
        PlayerPrefs.Save();
        savedBGMVolume = tempBGMVolume;
    
        Debug.Log("사운드 설정 저장 완료");
    }

    // 소리설정 UI 닫힐 때 호출 (ESC 등)
    public void RestoreVolumeIfNotSaved()
    {
        tempBGMVolume = savedBGMVolume;
    
        slider1.value = savedBGMVolume;

        SetBGMVolume(savedBGMVolume);
  
        UpdateSoundWave1();
        
    }



    //사운드 관련 매서드
    void UpdateSoundWave1()
    {
        float v = slider1.value;
        if (v == 0) soundWaveImg1.sprite = soundWaves1[0];
        else if (v < 0.25f) soundWaveImg1.sprite = soundWaves1[1];
        else if (v < 0.5f) soundWaveImg1.sprite = soundWaves1[2];
        else if (v >= 0.75f) soundWaveImg1.sprite = soundWaves1[3];
    }
    



}

