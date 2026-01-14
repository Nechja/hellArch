"""E2E test execution with Playwright browser automation."""

import socket
import time
from urllib.parse import urlparse
from uuid import uuid4

import httpx
import structlog
from playwright.async_api import Browser, Page, async_playwright

from testing.config import E2eStepConfig, E2eTestConfig, Settings
from testing.models import E2eScreenshot, E2eTestOutcome, TestResult

logger = structlog.get_logger()


class E2eExecutor:
    """Executes E2E browser tests using Playwright."""

    def __init__(self, settings: Settings, timeout: float = 30.0) -> None:
        self.settings = settings
        self.timeout = timeout * 1000  # Playwright uses milliseconds
        self._browser: Browser | None = None
        self._playwright = None
        self._token: str | None = None

    async def _ensure_browser(self) -> Browser:
        """Ensure browser is launched."""
        if self._browser is None or not self._browser.is_connected():
            self._playwright = await async_playwright().start()
            self._browser = await self._playwright.chromium.launch(
                headless=True,
                args=[
                    "--no-sandbox",
                    "--disable-dev-shm-usage",
                    "--disable-setuid-sandbox",
                    "--disable-gpu",
                    "--disable-software-rasterizer",
                    "--disable-extensions",
                    "--disable-background-networking",
                    "--disable-sync",
                    "--no-first-run",
                    "--ignore-certificate-errors",
                ],
            )
        return self._browser

    def _resolve_url(self, url: str) -> str:
        """Resolve hostname to IP for Docker internal DNS compatibility."""
        try:
            parsed = urlparse(url)
            if parsed.hostname and not parsed.hostname.replace(".", "").isdigit():
                ip = socket.gethostbyname(parsed.hostname)
                port = f":{parsed.port}" if parsed.port else ""
                return f"{parsed.scheme}://{ip}{port}{parsed.path or '/'}"
        except socket.gaierror:
            logger.debug("DNS resolution failed, using original URL", url=url)
        return url

    async def _get_token(self) -> str | None:
        """Get auth token for file uploads."""
        if self._token:
            return self._token

        client_id = self.settings.client_id or "testing-service"
        client_secret = self.settings.client_secret or "testing-secret"

        try:
            async with httpx.AsyncClient(timeout=10.0) as client:
                # Auth service is at port 5001 internally
                response = await client.post(
                    "http://auth:5001/auth/token",
                    data={
                        "grant_type": "client_credentials",
                        "client_id": client_id,
                        "client_secret": client_secret,
                    },
                )
                if response.status_code == 200:
                    self._token = response.json().get("access_token")
                    return self._token
        except httpx.HTTPError as e:
            logger.warning("Failed to get auth token for E2E", error=str(e))
        return None

    async def _upload_screenshot(
        self,
        screenshot_bytes: bytes,
        name: str,
    ) -> E2eScreenshot | None:
        """Upload screenshot to files service."""
        try:
            token = await self._get_token()
            headers = {"Authorization": f"Bearer {token}"} if token else {}

            owner_id = f"service:{self.settings.client_id or 'testing-service'}"
            filename = f"e2e-{name}-{uuid4().hex[:8]}.png"

            async with httpx.AsyncClient(timeout=30.0) as client:
                response = await client.post(
                    f"{self.settings.files_url}/api/files/upload",
                    files={"file": (filename, screenshot_bytes, "image/png")},
                    data={"owner_id": owner_id},
                    headers=headers,
                )

            if response.status_code in (200, 201):
                data = response.json()
                file_id = data.get("id")
                return E2eScreenshot(
                    step_name=name,
                    file_id=file_id,
                    file_url=f"/api/files/{file_id}",
                )
            logger.warning(
                "Screenshot upload failed",
                status=response.status_code,
                body=response.text[:200],
            )
        except Exception as e:
            logger.error("Screenshot upload error", error=str(e))
        return None

    async def _take_screenshot(
        self,
        page: Page,
        name: str,
    ) -> E2eScreenshot | None:
        """Take and upload a screenshot."""
        try:
            screenshot_bytes = await page.screenshot(full_page=True)
            return await self._upload_screenshot(screenshot_bytes, name)
        except Exception as e:
            logger.warning("Failed to take screenshot", error=str(e))
            return None

    async def _execute_step(
        self,
        page: Page,
        step: E2eStepConfig,
        base_url: str,
    ) -> tuple[bool, str | None]:
        """Execute a single E2E step."""
        try:
            match step.action:
                case "navigate":
                    url = step.value or ""
                    if url.startswith("/"):
                        url = base_url.rstrip("/") + url
                    elif not url.startswith("http"):
                        url = base_url.rstrip("/") + "/" + url
                    url = self._resolve_url(url)
                    await page.goto(url, wait_until="networkidle")
                case "click":
                    if step.selector:
                        await page.click(step.selector, timeout=self.timeout)
                case "fill":
                    if step.selector and step.value is not None:
                        await page.fill(step.selector, step.value, timeout=self.timeout)
                case "type":
                    if step.selector and step.value is not None:
                        await page.type(step.selector, step.value, timeout=self.timeout)
                case "wait":
                    if step.selector:
                        await page.wait_for_selector(step.selector, timeout=self.timeout)
                case "wait_time":
                    wait_ms = int(step.value or "1000")
                    await page.wait_for_timeout(wait_ms)
                case "assert_visible":
                    if step.selector:
                        await page.wait_for_selector(
                            step.selector, state="visible", timeout=self.timeout
                        )
                case "assert_text":
                    if step.selector:
                        locator = page.locator(step.selector)
                        await locator.wait_for(timeout=self.timeout)
                        text = await locator.text_content()
                        if step.value and step.value not in (text or ""):
                            return False, f"Expected text '{step.value}' not found in '{text}'"
                case "assert_url":
                    current_url = page.url
                    if step.value and step.value not in current_url:
                        return False, f"Expected URL to contain '{step.value}', got '{current_url}'"
                case "screenshot":
                    pass  # Screenshots handled separately
                case _:
                    return False, f"Unknown action: {step.action}"
            return True, None
        except Exception as e:
            return False, str(e)

    async def run_e2e_test(self, test: E2eTestConfig) -> E2eTestOutcome:
        """Run a complete E2E test."""
        start = time.monotonic()
        screenshots: list[E2eScreenshot] = []

        try:
            browser = await self._ensure_browser()
            context = await browser.new_context(viewport={"width": 1280, "height": 720})
            page = await context.new_page()

            failed_step: str | None = None
            error: str | None = None

            for i, step in enumerate(test.steps):
                step_name = step.screenshot_name or f"step_{i}_{step.action}"

                success, step_error = await self._execute_step(page, step, test.base_url)

                # Take screenshot if requested or on failure
                if step.action == "screenshot":
                    screenshot = await self._take_screenshot(page, step_name)
                    if screenshot:
                        screenshots.append(screenshot)
                elif not success and test.screenshot_on_failure:
                    screenshot = await self._take_screenshot(page, f"failure_{step_name}")
                    if screenshot:
                        screenshots.append(screenshot)

                if not success:
                    failed_step = step_name
                    error = step_error
                    break

            await context.close()

            duration_ms = int((time.monotonic() - start) * 1000)

            if failed_step:
                return E2eTestOutcome(
                    test_name=test.name,
                    result=TestResult.FAILED,
                    duration_ms=duration_ms,
                    failed_step=failed_step,
                    error=error,
                    screenshots=screenshots,
                    critical=test.critical,
                )

            return E2eTestOutcome(
                test_name=test.name,
                result=TestResult.PASSED,
                duration_ms=duration_ms,
                screenshots=screenshots,
                critical=test.critical,
            )

        except Exception as e:
            duration_ms = int((time.monotonic() - start) * 1000)
            logger.error("E2E test error", test=test.name, error=str(e))

            return E2eTestOutcome(
                test_name=test.name,
                result=TestResult.ERROR,
                duration_ms=duration_ms,
                error=str(e),
                screenshots=screenshots,
                critical=test.critical,
            )

    async def close(self) -> None:
        """Close browser resources."""
        if self._browser:
            await self._browser.close()
            self._browser = None
        if self._playwright:
            await self._playwright.stop()
            self._playwright = None
