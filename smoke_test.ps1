Write-Host "Running smoke test confirming NATS super cluster set up..."
# 
# function Assert-Value {
#     param(
#     $property
#     $value
#     $
#     )
# }

$connectionStats = Invoke-RestMethod "http://localhost:8222/connz"

if ($connectionStats.num_connections -ne 5) { throw "Number of connections should be 5" }

$connectionStats.connections