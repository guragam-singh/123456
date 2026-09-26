param([string]$ProjectRoot)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Speech
$output = Join-Path $ProjectRoot 'Assets\Audio\Voice'
[System.IO.Directory]::CreateDirectory($output) | Out-Null
$speaker = New-Object System.Speech.Synthesis.SpeechSynthesizer
try {
    $male = $speaker.GetInstalledVoices() | Where-Object { $_.Enabled -and $_.VoiceInfo.Gender -eq 'Male' -and $_.VoiceInfo.Culture.Name -like 'en-*' } | Select-Object -First 1
    if ($male) { $speaker.SelectVoice($male.VoiceInfo.Name) }
    $speaker.Rate = -2
    $speaker.Volume = 85
    $lines = @(
        'You learned the sky from one small branch.',
        'The rain. The warm afternoons. The familiar voice beneath you.',
        'When the wind came, you held on. Again, and again.',
        'You thought leaving would undo everything that made this home.',
        'But the branch has not forgotten. And you have not failed it.',
        'What held you is still part of you. Even now. Even here.'
    )
    for ($i = 0; $i -lt $lines.Length; $i++) {
        $path = Join-Path $output ('PLACEHOLDER_SyntheticNarration_{0:D2}.wav' -f ($i + 1))
        $speaker.SetOutputToWaveFile($path)
        $speaker.Speak($lines[$i])
        $speaker.SetOutputToNull()
    }
    'Generated six clearly labeled synthetic narration placeholders using ' + $speaker.Voice.Name
}
finally { $speaker.Dispose() }
