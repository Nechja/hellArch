import birl
import gleam/option
import gleeunit
import gleeunit/should
import health/types.{
  type HealthCheck, type ServiceStatus, type Status, type StatusConfig,
  Degraded, HealthCheck, Healthy, ServiceStatus, StatusConfig, Unhealthy,
}

pub fn main() {
  gleeunit.main()
}

// --- Test Helpers ---

fn make_check(status: Status, latency_ms: Int) -> HealthCheck {
  HealthCheck(
    status: status,
    latency_ms: latency_ms,
    checked_at: birl.now(),
    error: option.None,
  )
}

fn make_service_status(name: String, status: Status) -> ServiceStatus {
  ServiceStatus(
    name: name,
    status: status,
    latency_ms: 50,
    last_check: birl.now(),
    error: option.None,
    history: [],
  )
}

fn test_config() -> StatusConfig {
  StatusConfig(
    unhealthy_failure_threshold: 3,
    degraded_latency_threshold_ms: 100,
    history_depth: 10,
  )
}

// --- combine_status tests ---

pub fn combine_status_healthy_healthy_test() {
  types.combine_status(Healthy, Healthy)
  |> should.equal(Healthy)
}

pub fn combine_status_healthy_degraded_test() {
  types.combine_status(Healthy, Degraded)
  |> should.equal(Degraded)
}

pub fn combine_status_degraded_healthy_test() {
  types.combine_status(Degraded, Healthy)
  |> should.equal(Degraded)
}

pub fn combine_status_healthy_unhealthy_test() {
  types.combine_status(Healthy, Unhealthy)
  |> should.equal(Unhealthy)
}

pub fn combine_status_unhealthy_healthy_test() {
  types.combine_status(Unhealthy, Healthy)
  |> should.equal(Unhealthy)
}

pub fn combine_status_degraded_degraded_test() {
  types.combine_status(Degraded, Degraded)
  |> should.equal(Degraded)
}

pub fn combine_status_degraded_unhealthy_test() {
  types.combine_status(Degraded, Unhealthy)
  |> should.equal(Unhealthy)
}

pub fn combine_status_unhealthy_degraded_test() {
  types.combine_status(Unhealthy, Degraded)
  |> should.equal(Unhealthy)
}

pub fn combine_status_unhealthy_unhealthy_test() {
  types.combine_status(Unhealthy, Unhealthy)
  |> should.equal(Unhealthy)
}

// --- count_consecutive_failures tests ---

pub fn count_consecutive_failures_empty_history_test() {
  types.count_consecutive_failures([])
  |> should.equal(0)
}

pub fn count_consecutive_failures_no_failures_test() {
  let history = [
    make_check(Healthy, 50),
    make_check(Healthy, 45),
    make_check(Healthy, 55),
  ]

  types.count_consecutive_failures(history)
  |> should.equal(0)
}

pub fn count_consecutive_failures_one_failure_test() {
  let history = [
    make_check(Unhealthy, 0),
    make_check(Healthy, 50),
    make_check(Healthy, 45),
  ]

  types.count_consecutive_failures(history)
  |> should.equal(1)
}

pub fn count_consecutive_failures_multiple_failures_test() {
  let history = [
    make_check(Unhealthy, 0),
    make_check(Unhealthy, 0),
    make_check(Unhealthy, 0),
    make_check(Healthy, 50),
  ]

  types.count_consecutive_failures(history)
  |> should.equal(3)
}

pub fn count_consecutive_failures_all_failures_test() {
  let history = [
    make_check(Unhealthy, 0),
    make_check(Unhealthy, 0),
    make_check(Unhealthy, 0),
  ]

  types.count_consecutive_failures(history)
  |> should.equal(3)
}

pub fn count_consecutive_failures_failure_not_at_start_test() {
  let history = [
    make_check(Healthy, 50),
    make_check(Unhealthy, 0),
    make_check(Unhealthy, 0),
  ]

  types.count_consecutive_failures(history)
  |> should.equal(0)
}

pub fn count_consecutive_failures_degraded_not_counted_test() {
  let history = [
    make_check(Degraded, 150),
    make_check(Unhealthy, 0),
    make_check(Healthy, 50),
  ]

  types.count_consecutive_failures(history)
  |> should.equal(0)
}

// --- calculate_status tests ---

pub fn calculate_status_empty_history_healthy_test() {
  types.calculate_status([], test_config())
  |> should.equal(Healthy)
}

pub fn calculate_status_all_healthy_test() {
  let history = [
    make_check(Healthy, 50),
    make_check(Healthy, 45),
    make_check(Healthy, 55),
  ]

  types.calculate_status(history, test_config())
  |> should.equal(Healthy)
}

pub fn calculate_status_single_failure_degraded_test() {
  let history = [
    make_check(Unhealthy, 0),
    make_check(Healthy, 50),
    make_check(Healthy, 45),
  ]

  types.calculate_status(history, test_config())
  |> should.equal(Degraded)
}

pub fn calculate_status_two_failures_degraded_test() {
  let history = [
    make_check(Unhealthy, 0),
    make_check(Unhealthy, 0),
    make_check(Healthy, 50),
  ]

  types.calculate_status(history, test_config())
  |> should.equal(Degraded)
}

pub fn calculate_status_threshold_failures_unhealthy_test() {
  let history = [
    make_check(Unhealthy, 0),
    make_check(Unhealthy, 0),
    make_check(Unhealthy, 0),
    make_check(Healthy, 50),
  ]

  types.calculate_status(history, test_config())
  |> should.equal(Unhealthy)
}

pub fn calculate_status_slow_response_degraded_test() {
  let history = [
    make_check(Healthy, 150),
    make_check(Healthy, 50),
    make_check(Healthy, 45),
  ]

  types.calculate_status(history, test_config())
  |> should.equal(Degraded)
}

pub fn calculate_status_at_latency_threshold_healthy_test() {
  let history = [
    make_check(Healthy, 100),
    make_check(Healthy, 50),
  ]

  types.calculate_status(history, test_config())
  |> should.equal(Healthy)
}

pub fn calculate_status_custom_threshold_test() {
  let config =
    StatusConfig(
      unhealthy_failure_threshold: 2,
      degraded_latency_threshold_ms: 50,
      history_depth: 5,
    )

  let history = [
    make_check(Unhealthy, 0),
    make_check(Unhealthy, 0),
    make_check(Healthy, 30),
  ]

  types.calculate_status(history, config)
  |> should.equal(Unhealthy)
}

pub fn calculate_status_custom_latency_threshold_test() {
  let config =
    StatusConfig(
      unhealthy_failure_threshold: 3,
      degraded_latency_threshold_ms: 50,
      history_depth: 5,
    )

  let history = [
    make_check(Healthy, 60),
    make_check(Healthy, 30),
  ]

  types.calculate_status(history, config)
  |> should.equal(Degraded)
}

pub fn calculate_status_failure_takes_precedence_over_latency_test() {
  let history = [
    make_check(Unhealthy, 150),
    make_check(Unhealthy, 150),
    make_check(Unhealthy, 150),
  ]

  types.calculate_status(history, test_config())
  |> should.equal(Unhealthy)
}

// --- aggregate_services tests ---

pub fn aggregate_services_empty_list_test() {
  types.aggregate_services([])
  |> should.equal(Healthy)
}

pub fn aggregate_services_single_healthy_test() {
  let services = [make_service_status("api", Healthy)]

  types.aggregate_services(services)
  |> should.equal(Healthy)
}

pub fn aggregate_services_all_healthy_test() {
  let services = [
    make_service_status("api", Healthy),
    make_service_status("db", Healthy),
    make_service_status("cache", Healthy),
  ]

  types.aggregate_services(services)
  |> should.equal(Healthy)
}

pub fn aggregate_services_one_degraded_test() {
  let services = [
    make_service_status("api", Healthy),
    make_service_status("db", Degraded),
    make_service_status("cache", Healthy),
  ]

  types.aggregate_services(services)
  |> should.equal(Degraded)
}

pub fn aggregate_services_one_unhealthy_test() {
  let services = [
    make_service_status("api", Healthy),
    make_service_status("db", Unhealthy),
    make_service_status("cache", Healthy),
  ]

  types.aggregate_services(services)
  |> should.equal(Unhealthy)
}

pub fn aggregate_services_mixed_degraded_unhealthy_test() {
  let services = [
    make_service_status("api", Degraded),
    make_service_status("db", Unhealthy),
    make_service_status("cache", Healthy),
  ]

  types.aggregate_services(services)
  |> should.equal(Unhealthy)
}

pub fn aggregate_services_all_degraded_test() {
  let services = [
    make_service_status("api", Degraded),
    make_service_status("db", Degraded),
  ]

  types.aggregate_services(services)
  |> should.equal(Degraded)
}

pub fn aggregate_services_all_unhealthy_test() {
  let services = [
    make_service_status("api", Unhealthy),
    make_service_status("db", Unhealthy),
  ]

  types.aggregate_services(services)
  |> should.equal(Unhealthy)
}

// --- Edge case tests for status calculation ---

pub fn status_transitions_healthy_to_degraded_test() {
  let healthy_history = [
    make_check(Healthy, 50),
    make_check(Healthy, 45),
  ]

  types.calculate_status(healthy_history, test_config())
  |> should.equal(Healthy)

  let degraded_history = [
    make_check(Unhealthy, 0),
    make_check(Healthy, 50),
    make_check(Healthy, 45),
  ]

  types.calculate_status(degraded_history, test_config())
  |> should.equal(Degraded)
}

pub fn status_history_order_matters_test() {
  let failures_first = [
    make_check(Unhealthy, 0),
    make_check(Unhealthy, 0),
    make_check(Healthy, 50),
  ]

  let failures_last = [
    make_check(Healthy, 50),
    make_check(Unhealthy, 0),
    make_check(Unhealthy, 0),
  ]

  types.calculate_status(failures_first, test_config())
  |> should.equal(Degraded)

  types.calculate_status(failures_last, test_config())
  |> should.equal(Degraded)
}

pub fn old_failures_ignored_when_recent_checks_healthy_test() {
  // Tests that only the most recent N checks matter for failure threshold
  // (where N = unhealthy_failure_threshold)
  let config =
    StatusConfig(
      unhealthy_failure_threshold: 2,
      degraded_latency_threshold_ms: 100,
      history_depth: 5,
    )

  // Recent checks (first 2) are healthy, old failures at end don't trigger unhealthy
  let history = [
    make_check(Healthy, 50),
    make_check(Healthy, 50),
    make_check(Unhealthy, 0),
    make_check(Unhealthy, 0),
    make_check(Unhealthy, 0),
  ]

  types.calculate_status(history, config)
  |> should.equal(Healthy)
}
