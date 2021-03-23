using IplanHEE.Admin.Domain.ApprovalTree;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Runtime.InteropServices.ComTypes;

namespace IplanHEE.Admin.Infrastructure.Domain.ApprovalTree
{
    public class TreeEntityTypeConfiguration : IEntityTypeConfiguration<Tree>
    {
        public void Configure(EntityTypeBuilder<Tree> builder)
        {
            builder.ToTable("Trees", "ApprovalTree");
            builder.Property<TreeId>("_id").HasColumnName("Id");
            builder.HasKey("_id");
            builder.Property<string>("_name").HasColumnName("Name");
            builder.Property<string>("_label").HasColumnName("Label");
            builder.OwnsMany<SubTree>("_subTrees", st =>
            {
                st.ToTable("SubTrees", "ApprovalTree");
                st.Property<TreeId>("_treeId").HasColumnName("TreeId");
                st.WithOwner().HasForeignKey("_treeId");

                st.Property<string>("Name");
                st.Property<string>("_label").HasColumnName("Label");
                st.Property<bool>("IsDataNodeEnable").HasColumnName("IsDataNodeEnable");
                st.Property<bool>("_isLeafSubTree").HasColumnName("IsLeafSubTree");
                st.Property<bool>("IsPushDown");
                st.Property<SubTreeId>("Id");
                st.HasKey("Id");
                st.Property<int>("SubTreeSeedValue");
                st.Property<SubTreeId>("ParentSubTreeId").HasColumnName("ParentSubTreeId");

                st.OwnsMany<Node>("NodeList", n =>
                {
                    n.ToTable("Nodes", "ApprovalTree");
                    n.Property<SubTreeId>("_subtreeId").HasColumnName("SubTreeId");
                    n.WithOwner().HasForeignKey("_subtreeId");

                    n.Property<int>("NodeIndex");
                    n.Property<string>("_approvalLabel").HasColumnName("ApprovalLabel");
                    n.Property<string>("_rejectLabel").HasColumnName("RejectLabel");
                    n.Property<int>("NodeLevel");
                    n.Property<int>("_nodeMappingLevel").HasColumnName("NodeMappingLevel");

                    n.Property<NodeId>("Id");
                    n.HasKey("Id");
                    n.Property<bool>("IsAuthorNode");
                    n.Property<bool>("IsLeafNode");

                    n.Property<string>("Name");
                    n.Property<bool>("IsPublish");

                    n.OwnsMany<NodeRole>("_nodeRoles", nr =>
                    {
                        nr.ToTable("NodeRoles", "ApprovalTree");
                        nr.Property<NodeId>("_nodeId").HasColumnName("NodeId");
                        nr.WithOwner().HasForeignKey("_nodeId");
                        nr.Property<int>("Id").ValueGeneratedOnAdd();
                        nr.HasKey("Id");
                        nr.Property<string>("_role").HasColumnName("Role");
                        nr.Property<bool>("_isViewOnly").HasColumnName("IsViewOnly");
                        nr.Property<bool>("_isRemoved").HasColumnName("IsRemoved");
                    });

                });
            });
        }
    }

}
