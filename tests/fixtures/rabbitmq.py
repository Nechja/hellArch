import json
import os
from collections.abc import AsyncGenerator
from contextlib import asynccontextmanager
from dataclasses import dataclass, field
from datetime import UTC, datetime
from typing import Any
from uuid import uuid4

import pytest_asyncio
from aio_pika import Message, connect_robust
from aio_pika.abc import AbstractChannel, AbstractConnection, AbstractQueue


def get_rabbitmq_url() -> str:
    return os.getenv("RABBITMQ_URL", "amqp://guest:guest@localhost:5672/")


@asynccontextmanager
async def rabbitmq_connection() -> AsyncGenerator[AbstractConnection, None]:
    connection = await connect_robust(get_rabbitmq_url())
    try:
        yield connection
    finally:
        await connection.close()


@asynccontextmanager
async def rabbitmq_channel(connection: AbstractConnection) -> AsyncGenerator[AbstractChannel, None]:
    channel = await connection.channel()
    try:
        yield channel
    finally:
        await channel.close()


@pytest_asyncio.fixture
async def mq_connection() -> AsyncGenerator[AbstractConnection, None]:
    async with rabbitmq_connection() as conn:
        yield conn


@pytest_asyncio.fixture
async def mq_channel(mq_connection: AbstractConnection) -> AsyncGenerator[AbstractChannel, None]:
    async with rabbitmq_channel(mq_connection) as channel:
        yield channel


@dataclass
class CloudEvent:
    """Represents a CloudEvents 1.0 message."""

    type: str
    source: str
    data: dict[str, Any]
    id: str = field(default_factory=lambda: str(uuid4()))
    specversion: str = "1.0"
    time: str = field(default_factory=lambda: datetime.now(UTC).isoformat())
    datacontenttype: str = "application/json"

    def to_dict(self) -> dict[str, Any]:
        return {
            "specversion": self.specversion,
            "type": self.type,
            "source": self.source,
            "id": self.id,
            "time": self.time,
            "datacontenttype": self.datacontenttype,
            "data": self.data,
        }

    def to_bytes(self) -> bytes:
        return json.dumps(self.to_dict()).encode()


@dataclass
class ReceivedMessage:
    """Represents a received message from the queue."""

    body: bytes
    routing_key: str
    exchange: str
    headers: dict[str, Any] | None = None

    @property
    def json(self) -> dict[str, Any]:
        return json.loads(self.body.decode())

    @property
    def cloud_event(self) -> CloudEvent:
        data = self.json
        return CloudEvent(
            type=data["type"],
            source=data["source"],
            data=data["data"],
            id=data["id"],
            specversion=data.get("specversion", "1.0"),
            time=data.get("time", ""),
            datacontenttype=data.get("datacontenttype", "application/json"),
        )


class RabbitMQTestHelper:
    """Helper class for common RabbitMQ testing operations."""

    def __init__(self, channel: AbstractChannel):
        self._channel = channel
        self._queues: dict[str, AbstractQueue] = {}

    async def declare_queue(self, name: str, durable: bool = False) -> AbstractQueue:
        queue = await self._channel.declare_queue(name, durable=durable, auto_delete=True)
        self._queues[name] = queue
        return queue

    async def declare_exchange(self, name: str, exchange_type: str = "topic") -> None:
        await self._channel.declare_exchange(name, type=exchange_type, durable=False, auto_delete=True)

    async def bind_queue(self, queue_name: str, exchange_name: str, routing_key: str) -> None:
        queue = self._queues.get(queue_name)
        if not queue:
            queue = await self.declare_queue(queue_name)
        await queue.bind(exchange_name, routing_key)

    async def publish(
        self,
        exchange_name: str,
        routing_key: str,
        body: bytes | str | dict,
        headers: dict[str, Any] | None = None,
    ) -> None:
        exchange = await self._channel.get_exchange(exchange_name)

        if isinstance(body, dict):
            body = json.dumps(body).encode()
        elif isinstance(body, str):
            body = body.encode()

        message = Message(body=body, headers=headers)
        await exchange.publish(message, routing_key=routing_key)

    async def publish_cloud_event(
        self,
        exchange_name: str,
        routing_key: str,
        event: CloudEvent,
    ) -> None:
        await self.publish(exchange_name, routing_key, event.to_bytes())

    async def consume_one(self, queue_name: str, timeout: float = 5.0) -> ReceivedMessage | None:
        queue = self._queues.get(queue_name)
        if not queue:
            return None

        try:
            async with queue.iterator(timeout=timeout) as queue_iter:
                async for message in queue_iter:
                    async with message.process():
                        return ReceivedMessage(
                            body=message.body,
                            routing_key=message.routing_key or "",
                            exchange=message.exchange or "",
                            headers=dict(message.headers) if message.headers else None,
                        )
        except TimeoutError:
            return None
        return None

    async def consume_all(self, queue_name: str, timeout: float = 1.0) -> list[ReceivedMessage]:
        messages = []
        while True:
            msg = await self.consume_one(queue_name, timeout=timeout)
            if msg is None:
                break
            messages.append(msg)
        return messages

    async def purge_queue(self, queue_name: str) -> None:
        queue = self._queues.get(queue_name)
        if queue:
            await queue.purge()

    async def cleanup(self) -> None:
        for queue in self._queues.values():
            await queue.delete()
        self._queues.clear()


@pytest_asyncio.fixture
async def mq_helper(mq_channel: AbstractChannel) -> AsyncGenerator[RabbitMQTestHelper, None]:
    helper = RabbitMQTestHelper(mq_channel)
    yield helper
    await helper.cleanup()


def create_user_created_event(
    user_id: str,
    email: str,
    username: str,
    provider: str = "discord",
) -> CloudEvent:
    return CloudEvent(
        type="auth.user.created",
        source="/auth-service",
        data={
            "user_id": user_id,
            "email": email,
            "username": username,
            "provider": provider,
        },
    )


def create_user_updated_event(user_id: str, changes: dict[str, Any]) -> CloudEvent:
    return CloudEvent(
        type="auth.user.updated",
        source="/auth-service",
        data={
            "user_id": user_id,
            "changes": changes,
        },
    )


def create_session_created_event(
    session_id: str,
    user_id: str,
    ip_address: str = "127.0.0.1",
) -> CloudEvent:
    return CloudEvent(
        type="auth.session.created",
        source="/auth-service",
        data={
            "session_id": session_id,
            "user_id": user_id,
            "ip_address": ip_address,
        },
    )
