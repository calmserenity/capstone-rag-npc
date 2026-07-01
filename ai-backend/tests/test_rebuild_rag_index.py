import json
from pathlib import Path

from app.rag.vector_store import INDEX_FILENAME, METADATA_FILENAME
from scripts.rebuild_rag_index import rebuild_rag_index


def test_rebuild_rag_index_loads_chunks_embeds_and_saves_index(tmp_path: Path):
    knowledge_base = tmp_path / "knowledge-base"
    output_dir = tmp_path / "faiss"
    npc_dir = knowledge_base / "npc"
    puzzle_dir = knowledge_base / "puzzles"
    npc_dir.mkdir(parents=True)
    puzzle_dir.mkdir(parents=True)
    (npc_dir / "rock.md").write_text("Rock gives sleepy hints.", encoding="utf-8")
    (puzzle_dir / "pond.md").write_text(
        "It reflects the sky, but it is not a mirror.",
        encoding="utf-8",
    )

    summary = rebuild_rag_index(
        knowledge_base_path=knowledge_base,
        output_dir=output_dir,
        embedding_function=lambda text: [float(len(text)), 1.0],
    )
    metadata = json.loads((output_dir / METADATA_FILENAME).read_text(encoding="utf-8"))

    assert summary.documents_loaded == 2
    assert summary.chunks_created == 2
    assert summary.embeddings_generated == 2
    assert summary.index_total == 2
    assert (output_dir / INDEX_FILENAME).exists()
    assert (output_dir / METADATA_FILENAME).exists()
    assert [record["document_id"] for record in metadata] == [
        "npc/rock.md",
        "puzzles/pond.md",
    ]
