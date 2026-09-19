# Repository instructions

## Messaging tasks and Git workflow

- For Messaging roadmap work, read `docs/messaging-roadmap.md`, especially section 17, before implementation. Keep work scoped to the assigned task and its dependencies.
- The user has authorized automatic task branch creation, task-scoped commits, pushes to `origin`, and opening/updating task pull requests when implementing assigned tasks. Do not ask for this authorization again. This does not automatically start other roadmap tasks.
- Use the branch mapping in section 17.3. One task normally has one branch and one PR; subtasks share the task branch. Inspect existing branches and changes before creating or resuming a branch.
- Fetch before selecting the base. Prefer current `origin/main` once dependencies are merged. For stacked work, document the dependency and target its branch until it merges.
- Preserve unrelated user changes. Use an isolated worktree in an allowed writable location when necessary; do not automatically stash, reset, overwrite, or commit unrelated files.
- Implement, review, and run checks appropriate to the change before committing. Stage only task files/hunks. Use commit messages such as `feat(messaging): P2-T01 persist messages with idempotency`.
- Push the task branch with an upstream, verify the result, and create/update a PR when tooling and authentication permit. Use a draft PR for incomplete work and report failing or unavailable checks honestly.
- Report task ID, branch/base, commit, checks, push status, PR URL, and remaining work. If authentication, network, or tooling prevents a step, report the exact blocker; never claim an uncompleted push or PR succeeded.
- This workflow does not authorize merging PRs, pushing directly to `main`, force-pushing, deleting branches, or deploying. Follow any explicit subsequent user authorization for those actions.
