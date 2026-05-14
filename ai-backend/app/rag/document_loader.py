from dataclasses import dataclass
from pathlib import Path


SUPPORTED_EXTENSIONS = {".md", ".txt"}
DEFAULT_KNOWLEDGE_BASE_PATH = Path("/app/knowledge-base")


@dataclass(frozen=True)
class KnowledgeDocument:
    id: str
    category: str
    source: str
    text: str


def load_knowledge_documents(
    knowledge_base_path: Path = DEFAULT_KNOWLEDGE_BASE_PATH,
) -> list[KnowledgeDocument]:
    """Load Markdown and text documents from the knowledge base."""
    if not knowledge_base_path.exists():
        return []

    documents: list[KnowledgeDocument] = []
    for path in sorted(knowledge_base_path.rglob("*")):
        if not path.is_file() or path.suffix.lower() not in SUPPORTED_EXTENSIONS:
            continue

        relative_path = path.relative_to(knowledge_base_path)
        if len(relative_path.parts) < 2:
            continue

        text = path.read_text(encoding="utf-8").strip()
        if not text:
            continue

        documents.append(
            KnowledgeDocument(
                id=relative_path.as_posix(),
                category=relative_path.parts[0] if len(relative_path.parts) > 1 else "",
                source=path.name,
                text=text,
            )
        )

    return documents
