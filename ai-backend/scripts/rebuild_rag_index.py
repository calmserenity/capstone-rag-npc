from dataclasses import dataclass
from pathlib import Path
import sys


BACKEND_ROOT = Path(__file__).resolve().parents[1]
PROJECT_ROOT = BACKEND_ROOT.parent
if str(BACKEND_ROOT) not in sys.path:
    sys.path.insert(0, str(BACKEND_ROOT))

from app.rag.chunker import chunk_documents
from app.rag.document_loader import (
    DEFAULT_KNOWLEDGE_BASE_PATH,
    load_knowledge_documents,
)
from app.rag.embeddings import EmbeddingFunction, embed_chunks, embed_text
from app.rag.vector_store import (
    DEFAULT_FAISS_DIR,
    INDEX_FILENAME,
    METADATA_FILENAME,
    store_embeddings,
)


LOCAL_KNOWLEDGE_BASE_PATH = PROJECT_ROOT / "knowledge-base"
LOCAL_FAISS_DIR = BACKEND_ROOT / "data" / "faiss"


@dataclass(frozen=True)
class RebuildSummary:
    documents_loaded: int
    chunks_created: int
    embeddings_generated: int
    index_total: int
    index_path: Path
    metadata_path: Path


def rebuild_rag_index(
    knowledge_base_path: Path | None = None,
    output_dir: Path | None = None,
    embedding_function: EmbeddingFunction = embed_text,
) -> RebuildSummary:
    """Rebuild the saved FAISS index from knowledge-base documents."""
    knowledge_base_path = _resolve_knowledge_base_path(knowledge_base_path)
    output_dir = _resolve_output_dir(output_dir)

    documents = load_knowledge_documents(knowledge_base_path)
    chunks = chunk_documents(documents)
    embedded_chunks = embed_chunks(chunks, embedding_function)
    index = store_embeddings(embedded_chunks, output_dir)

    return RebuildSummary(
        documents_loaded=len(documents),
        chunks_created=len(chunks),
        embeddings_generated=len(embedded_chunks),
        index_total=index.ntotal,
        index_path=output_dir / INDEX_FILENAME,
        metadata_path=output_dir / METADATA_FILENAME,
    )


def _resolve_knowledge_base_path(path: Path | None) -> Path:
    if path is not None:
        return path
    if DEFAULT_KNOWLEDGE_BASE_PATH.exists():
        return DEFAULT_KNOWLEDGE_BASE_PATH
    return LOCAL_KNOWLEDGE_BASE_PATH


def _resolve_output_dir(path: Path | None) -> Path:
    if path is not None:
        return path
    if DEFAULT_KNOWLEDGE_BASE_PATH.exists():
        return DEFAULT_FAISS_DIR
    return LOCAL_FAISS_DIR


def main() -> None:
    summary = rebuild_rag_index()

    print(f"Loaded {summary.documents_loaded} documents.")
    print(f"Created {summary.chunks_created} chunks.")
    print(f"Generated {summary.embeddings_generated} embeddings.")
    print(f"Saved {summary.index_total} vectors.")
    print(f"Saved FAISS index to {summary.index_path}.")
    print(f"Saved metadata to {summary.metadata_path}.")


if __name__ == "__main__":
    main()
