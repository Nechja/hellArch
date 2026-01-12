"""Golden path test: File lifecycle."""

import pytest

from testing.clients import FilesClient


@pytest.mark.golden
async def test_file_lifecycle(
    files_client: FilesClient,
    auth_token: str | None,
) -> None:
    """Complete file upload/download/delete cycle."""
    test_content = b"Hello, HellArch testing!"
    test_filename = "test_golden_path.txt"

    # Step 1: Upload file
    upload_result = await files_client.upload(
        filename=test_filename,
        content=test_content,
        token=auth_token,
    )
    assert upload_result is not None, "File upload failed"

    file_id = upload_result.id
    assert file_id, "No file ID returned"

    try:
        # Step 2: Verify file exists in listing
        files = await files_client.list_files(token=auth_token)
        file_ids = [f.id for f in files]
        assert file_id in file_ids, "Uploaded file not found in listing"

        # Step 3: Download and verify content
        downloaded = await files_client.download(file_id, token=auth_token)
        assert downloaded is not None, "File download failed"
        assert downloaded == test_content, "Downloaded content doesn't match"

    finally:
        # Step 4: Clean up - delete file
        deleted = await files_client.remove(file_id, token=auth_token)
        assert deleted, "File deletion failed"


@pytest.mark.golden
class TestFileLifecycle:
    """File lifecycle tests as separate steps."""

    _file_id: str | None = None
    _test_content: bytes = b"Step-by-step test content"

    async def test_upload(self, files_client: FilesClient, auth_token: str | None) -> None:
        """Step 1: Upload a file."""
        result = await files_client.upload(
            filename="step_test.txt",
            content=self._test_content,
            token=auth_token,
        )
        assert result is not None
        TestFileLifecycle._file_id = result.id

    async def test_verify_exists(
        self, files_client: FilesClient, auth_token: str | None
    ) -> None:
        """Step 2: Verify file exists."""
        if not self._file_id:
            pytest.skip("No file ID from upload step")

        files = await files_client.list_files(token=auth_token)
        assert any(f.id == self._file_id for f in files)

    async def test_download(
        self, files_client: FilesClient, auth_token: str | None
    ) -> None:
        """Step 3: Download and verify."""
        if not self._file_id:
            pytest.skip("No file ID from upload step")

        content = await files_client.download(self._file_id, token=auth_token)
        assert content == self._test_content

    async def test_delete(self, files_client: FilesClient, auth_token: str | None) -> None:
        """Step 4: Delete file."""
        if not self._file_id:
            pytest.skip("No file ID from upload step")

        deleted = await files_client.remove(self._file_id, token=auth_token)
        assert deleted
