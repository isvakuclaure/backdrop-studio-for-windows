param([string]$ImagePath,[switch]$Check)
$ErrorActionPreference='Stop'
try {
 Add-Type -AssemblyName System.Runtime.WindowsRuntime
 $fileType=[Windows.Storage.StorageFile,Windows.Storage,ContentType=WindowsRuntime]
 $lockType=[Windows.System.UserProfile.LockScreen,Windows.System.UserProfile,ContentType=WindowsRuntime]
 $methods=[System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object { $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 }
 $generic=$methods | Where-Object { $_.IsGenericMethod -and $_.GetGenericArguments().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1' } | Select-Object -First 1
 $task=$generic.MakeGenericMethod($fileType).Invoke($null,@($fileType::GetFileFromPathAsync($ImagePath)))
 $file=$task.GetAwaiter().GetResult()
 $action=$methods | Where-Object { !$_.IsGenericMethod -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncAction' } | Select-Object -First 1
 if(!$action -or !$file -or !$lockType.GetMethod('SetImageFileAsync')) { throw 'La API de bloqueo no está disponible.' }
 if(!$Check){$job=$action.Invoke($null,@($lockType::SetImageFileAsync($file)));$job.GetAwaiter().GetResult()}
 exit 0
} catch { [Console]::Error.WriteLine($_.Exception.GetBaseException().Message);exit 1 }
