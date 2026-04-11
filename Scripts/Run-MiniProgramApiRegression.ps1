param(
    [string]$BaseUrl = "https://localhost:44332",
    [string]$ConnectionString = "Server=.;Database=StudentManagementDB;Integrated Security=True;Encrypt=False;MultipleActiveResultSets=True"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Data

function Invoke-ApiJson {
    param(
        [string]$Method,
        [string]$Url,
        [object]$Body = $null
    )

    $args = @("-k", "-sS", "-X", $Method, "-H", "Accept: application/json")
    if ($null -ne $Body) {
        $payload = $Body | ConvertTo-Json -Depth 6 -Compress
        $args += @("-H", "Content-Type: application/json; charset=utf-8", "--data-raw", $payload)
    }

    $args += @("-w", "`nHTTPSTATUS:%{http_code}", $Url)

    $output = & curl.exe @args 2>&1
    if ($LASTEXITCODE -ne 0) {
        return [pscustomobject]@{
            StatusCode = 0
            Data = $null
            ErrorMessage = ($output | Out-String).Trim()
        }
    }

    $text = ($output | Out-String)
    $marker = "HTTPSTATUS:"
    $index = $text.LastIndexOf($marker)
    if ($index -lt 0) {
        return [pscustomobject]@{
            StatusCode = 0
            Data = $null
            ErrorMessage = "curl output missing HTTP status marker"
        }
    }

    $bodyText = $text.Substring(0, $index).Trim()
    $statusText = $text.Substring($index + $marker.Length).Trim()
    $json = $null
    if ($bodyText) {
        try {
            $json = $bodyText | ConvertFrom-Json
        }
        catch {
            $json = [pscustomobject]@{ raw = $bodyText }
        }
    }

    return [pscustomobject]@{
        StatusCode = [int]$statusText
        Data = $json
        ErrorMessage = $null
    }
}

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

$conn = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
$conn.Open()

$courseId = $null
$studentUserId = 7
$teacherCourseId = 1

try {
    $createCommand = $conn.CreateCommand()
    $createCommand.CommandText = @"
DECLARE @CourseIdTable TABLE (CourseID INT);

INSERT INTO dbo.Courses (CourseName, Credits, TeacherID, CourseType)
OUTPUT INSERTED.CourseID INTO @CourseIdTable
VALUES (@courseName, 1.0, N'T003', 4);

DECLARE @NewCourseId INT = (SELECT TOP 1 CourseID FROM @CourseIdTable);

INSERT INTO dbo.ClassSessions (CourseID, DayOfWeek, StartPeriod, EndPeriod, Classroom, StartWeek, EndWeek)
VALUES (@NewCourseId, 1, 1, 2, N'AutoRegressionRoom', 1, 18);

SELECT @NewCourseId;
"@
    [void]$createCommand.Parameters.Add("@courseName", [System.Data.SqlDbType]::NVarChar, 100)
    $createCommand.Parameters["@courseName"].Value = "AutoRegressionConflict-" + [DateTime]::Now.ToString("yyyyMMddHHmmss")
    $courseId = [int]$createCommand.ExecuteScalar()

    $selectionResponse = Invoke-ApiJson -Method "POST" -Url ($BaseUrl.TrimEnd("/") + "/api/miniprogram/course-selection/select") -Body @{
        userId = $studentUserId
        courseId = $courseId
    }
    if ($selectionResponse.StatusCode -eq 0) {
        throw "Course-selection conflict test did not receive an HTTP response: $($selectionResponse.ErrorMessage)"
    }
    Assert-True ($selectionResponse.StatusCode -eq 400) "Course-selection conflict test failed: expected 400, actual $($selectionResponse.StatusCode)"
    $selectionMessage = [string]($selectionResponse.Data.message)
    Assert-True (-not [string]::IsNullOrWhiteSpace($selectionMessage)) "Course-selection conflict test failed: API returned 400 but message was empty"

    $gradePermissionResponse = Invoke-ApiJson -Method "GET" -Url ($BaseUrl.TrimEnd("/") + "/api/miniprogram/teacher-grade-entry?userId=7&courseId=$teacherCourseId")
    if ($gradePermissionResponse.StatusCode -eq 0) {
        throw "Teacher grade-entry permission test did not receive an HTTP response: $($gradePermissionResponse.ErrorMessage)"
    }
    Assert-True ($gradePermissionResponse.StatusCode -eq 400) "Teacher grade-entry permission test failed: expected 400, actual $($gradePermissionResponse.StatusCode)"

    $invalidUserResponse = Invoke-ApiJson -Method "GET" -Url ($BaseUrl.TrimEnd("/") + "/api/miniprogram/timetable?userId=999999")
    if ($invalidUserResponse.StatusCode -eq 0) {
        throw "Invalid-user access test did not receive an HTTP response: $($invalidUserResponse.ErrorMessage)"
    }
    Assert-True ($invalidUserResponse.StatusCode -eq 404) "Invalid-user access test failed: expected 404, actual $($invalidUserResponse.StatusCode)"

    Write-Output "MiniProgram API regression passed."
    Write-Output "1. Course-selection conflict: passed"
    Write-Output "2. Teacher grade-entry permission: passed"
    Write-Output "3. Invalid-user access: passed"
}
finally {
    if ($null -ne $courseId) {
        $cleanupCommand = $conn.CreateCommand()
        $cleanupCommand.CommandText = @"
DELETE FROM dbo.ClassSessions WHERE CourseID = @CourseId;
DELETE FROM dbo.StudentCourses WHERE CourseID = @CourseId;
DELETE FROM dbo.Courses WHERE CourseID = @CourseId;
"@
        [void]$cleanupCommand.Parameters.Add("@CourseId", [System.Data.SqlDbType]::Int)
        $cleanupCommand.Parameters["@CourseId"].Value = $courseId
        [void]$cleanupCommand.ExecuteNonQuery()
    }

    $conn.Close()
}
