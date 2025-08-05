using UnityEngine;
using TMPro;
using Fusion;
using System;

/// <summary>
/// �v���C���[�̃X�R�A�ȂǁA�Q�[�����̏�Ԃ�ێ�����V���v���ȃf�[�^���f���N���X�B
/// </summary>
public class PlayerModel
{
    private int _score;
    [SerializeField]
    public int score
    {
        get => _score;
        private set
        {
            if (_score != value)
            {
                _score = value;
                OnScoreChanged?.Invoke(_score);
            }
        }
    }
    public event Action<int> OnScoreChanged;

    public PlayerModel(int initialScore = 0)
    {
        score = initialScore;
    }

    // �X�R�A��ύX����
    public void SetScore(int newScore)
    {
        score = newScore;
    }
    // �X�R�A�����Z����
    public void AddScore(int amount)
    {
        score += amount;
    }
}

