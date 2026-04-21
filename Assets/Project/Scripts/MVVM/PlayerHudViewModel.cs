using System.ComponentModel;
using UnityEngine;
using UnityWeld.Binding;

[Binding]
public class PlayerHudViewModel : MonoBehaviour, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    private string ammoText;
    private string healthText;
    private string currentWaveText;
    private string scoreText;

    private float damageBuffFill;
    private float defenseBuffFill;
    private float speedBuffFill;
    private float fireRateBuffFill;

    [Binding]
    public string AmmoText
    {
        get { return ammoText; }
        set
        {
            if (ammoText == value)
            {
                return;
            }

            ammoText = value;
            RaisePropertyChanged(nameof(AmmoText));
        }
    }

    [Binding]
    public string HealthText
    {
        get { return healthText; }
        set
        {
            if (healthText == value)
            {
                return;
            }

            healthText = value;
            RaisePropertyChanged(nameof(HealthText));
        }
    }

    [Binding]
    public string CurrentWaveText
    {
        get { return currentWaveText; }
        set
        {
            if (currentWaveText == value)
            {
                return;
            }

            currentWaveText = value;
            RaisePropertyChanged(nameof(CurrentWaveText));
        }
    }

    [Binding]
    public string ScoreText
    {
        get { return scoreText; }
        set
        {
            if (scoreText == value)
            {
                return;
            }

            scoreText = value;
            RaisePropertyChanged(nameof(ScoreText));
        }
    }

    [Binding]
    public float DamageBuffFill
    {
        get { return damageBuffFill; }
        set
        {
            if (Mathf.Approximately(damageBuffFill, value))
            {
                return;
            }

            damageBuffFill = value;
            RaisePropertyChanged(nameof(DamageBuffFill));
        }
    }

    [Binding]
    public float DefenseBuffFill
    {
        get { return defenseBuffFill; }
        set
        {
            if (Mathf.Approximately(defenseBuffFill, value))
            {
                return;
            }

            defenseBuffFill = value;
            RaisePropertyChanged(nameof(DefenseBuffFill));
        }
    }

    [Binding]
    public float SpeedBuffFill
    {
        get { return speedBuffFill; }
        set
        {
            if (Mathf.Approximately(speedBuffFill, value))
            {
                return;
            }

            speedBuffFill = value;
            RaisePropertyChanged(nameof(SpeedBuffFill));
        }
    }

    [Binding]
    public float FireRateBuffFill
    {
        get { return fireRateBuffFill; }
        set
        {
            if (Mathf.Approximately(fireRateBuffFill, value))
            {
                return;
            }

            fireRateBuffFill = value;
            RaisePropertyChanged(nameof(FireRateBuffFill));
        }
    }

    private void RaisePropertyChanged(string propertyName)
    {
        if (PropertyChanged == null)
        {
            return;
        }

        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    }
}