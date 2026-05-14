from dataclasses import dataclass
import re

from app.rag.document_loader import KnowledgeDocument


DEFAULT_CHUNK_SIZE = 800


@dataclass(frozen=True)
class KnowledgeChunk:
    id: str
    document_id: str
    category: str
    source: str
    text: str
    chunk_index: int


def chunk_documents(
    documents: list[KnowledgeDocument],
    max_chars: int = DEFAULT_CHUNK_SIZE,
) -> list[KnowledgeChunk]:
    """Split loaded documents into retrieval-sized chunks."""
    chunks: list[KnowledgeChunk] = []

    for document in documents:
        text_chunks = _chunk_text(document.text, max_chars)
        for index, text in enumerate(text_chunks):
            chunks.append(
                KnowledgeChunk(
                    id=f"{document.id}#chunk-{index}",
                    document_id=document.id,
                    category=document.category,
                    source=document.source,
                    text=text,
                    chunk_index=index,
                )
            )

    return chunks


def _chunk_text(text: str, max_chars: int) -> list[str]:
    paragraphs = [
        paragraph.strip()
        for paragraph in re.split(r"\n\s*\n", text.strip())
        if paragraph.strip()
    ]

    chunks: list[str] = []
    current_parts: list[str] = []
    current_length = 0

    for paragraph in paragraphs:
        paragraph_parts = _split_oversized_paragraph(paragraph, max_chars)
        for part in paragraph_parts:
            part_length = len(part)
            separator_length = 2 if current_parts else 0

            if current_parts and current_length + separator_length + part_length > max_chars:
                chunks.append("\n\n".join(current_parts))
                current_parts = []
                current_length = 0

            current_parts.append(part)
            current_length += (2 if current_length else 0) + part_length

    if current_parts:
        chunks.append("\n\n".join(current_parts))

    return chunks


def _split_oversized_paragraph(paragraph: str, max_chars: int) -> list[str]:
    if len(paragraph) <= max_chars:
        return [paragraph]

    sentences = [
        sentence.strip()
        for sentence in re.split(r"(?<=[.!?])\s+", paragraph)
        if sentence.strip()
    ]

    parts: list[str] = []
    current = ""

    for sentence in sentences:
        if not current:
            current = sentence
            continue

        if len(current) + 1 + len(sentence) <= max_chars:
            current = f"{current} {sentence}"
        else:
            parts.extend(_split_by_length(current, max_chars))
            current = sentence

    if current:
        parts.extend(_split_by_length(current, max_chars))

    return parts


def _split_by_length(text: str, max_chars: int) -> list[str]:
    return [
        text[index : index + max_chars].strip()
        for index in range(0, len(text), max_chars)
        if text[index : index + max_chars].strip()
    ]
