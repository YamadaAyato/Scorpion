using UnityEngine;
using UnityEngine.UI;

public class PlayerUIBase : MonoBehaviour
{
    [Header("CharacterInfo")]
    [SerializeField] private CharacterInfo _info;

    [Header("絵")]
    [SerializeField] private Image _charaImage;

    [Header("オブジェクトの位置")]
    [SerializeField] private Transform _charaTransform;

    [Header("名前")]
    [SerializeField] private Text _text;

    [Header("決定表示")]
    [SerializeField] private Text _dText;

    [Header("左右移動の音")]
    [SerializeField] private AudioClip _moveClip;

    [Header("決定音")]
    [SerializeField] private AudioClip _decisionClip;

    [Header("戻る時の音")]
    [SerializeField] private AudioClip _returnClip;

    private int _index = 0;
    private GameObject _currentObject;
    private bool _isGo;
    private AudioSource _source;

    public bool IsGo => _isGo;

    private void Start()
    {
        CreateObject();
        _source = GetComponent<AudioSource>();
    }

    protected void Receive(string name)
    {
        switch (name)
        {
            case "Left":

                _source.PlayOneShot(_moveClip);
                _isGo = false;
                if (_index > 0)
                    _index--;
                else
                    _index = _info.CharaList.Count - 1;

                CreateObject();
                break;

            case "Right":

                _source.PlayOneShot(_moveClip);
                _isGo = false;
                if (_index < _info.CharaList.Count - 1)
                    _index++;
                else
                    _index = 0;

                CreateObject();
                break;

            case "Decision":
                _source.PlayOneShot(_decisionClip);
                _isGo = true;
                _dText.text = "決定！";
                break;
        }
    }

    protected void WReceive(string name)
    {
        switch (name)
        {
            case "Return":
                _source.PlayOneShot(_returnClip);
                _dText.text = null;
                break;
        }
    }

    private void CreateObject()
    {
        _charaImage.sprite = _info.CharaList[_index].Sprite;
        _text.text = _info.CharaList[_index].CharacterName;

        if (_currentObject != null)
        {
            var c = _currentObject;
            Destroy(c);
            _currentObject = null;
        }

        _currentObject = Instantiate(_info.CharaList[_index].Character, _charaTransform);
    }
}
