Start-Sleep -Seconds 10

$timeout = 60
$time = 0
while ($time -lt $timeout) {
    try {
        $service = Get-Service w32time -ErrorAction Stop
        if ($service.Status -eq 'Running') {
            w32tm /resync
            break
        }
    } catch {
        # Dienst noch nicht verfügbar
    }
    Start-Sleep -Seconds 5
    $time += 5
}
