resource "random_string" "resource_suffix" {
  length  = 6
  upper   = false
  special = false
  numeric = true
}
