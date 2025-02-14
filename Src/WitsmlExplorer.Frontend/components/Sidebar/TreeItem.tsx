import React, { useContext } from "react";
import { TreeItem } from "@material-ui/lab";
import styled from "styled-components";
import { TreeItemProps } from "@material-ui/lab/TreeItem";
import { colors } from "../../styles/Colors";
import { useTheme } from "@material-ui/core/styles";
import NavigationContext from "../../contexts/navigationContext";
import { ToggleTreeNodeAction } from "../../contexts/navigationStateReducer";
import NavigationType from "../../contexts/navigationType";
import Icon from "../../styles/Icons";
import { DotProgress } from "@equinor/eds-core-react";

interface StyledTreeItemProps extends TreeItemProps {
  labelText: string;
  selected?: boolean;
  isActive?: boolean;
  isLoading?: boolean;
}

const StyledTreeItem = (props: StyledTreeItemProps): React.ReactElement => {
  const { labelText, selected, isActive, isLoading, ...other } = props; // eslint-disable-line
  const { dispatchNavigation } = useContext(NavigationContext);
  const isCompactMode = useTheme().props.MuiCheckbox.size === "small";

  const toggleTreeNode = (props: StyledTreeItemProps) => {
    const toggleTreeNode: ToggleTreeNodeAction = { type: NavigationType.ToggleTreeNode, payload: { nodeId: props.nodeId } };
    dispatchNavigation(toggleTreeNode);
  };

  return (
    <TreeItem
      onIconClick={() => toggleTreeNode(props)}
      label={
        <Label>
          {isActive && <Icon name="isActive" color={colors.interactive.primaryResting} />}
          <NavigationDrawer selected={selected} compactMode={isCompactMode}>
            {labelText} {isLoading && <DotProgress color={"primary"} size={32} />}
          </NavigationDrawer>
        </Label>
      }
      {...other}
    />
  );
};

const Label = styled.div`
  display: flex;
`;

const NavigationDrawer = styled.p<{ selected: boolean; compactMode: boolean }>`
  color: ${(props) => (props.selected ? colors.interactive.primaryResting : colors.text.staticIconsDefault)};
  font-family: EquinorMedium, sans-serif;
  font-size: 0.75rem;
  line-height: 1rem;
  padding: ${(props) => (props.compactMode ? "0.5rem" : "1rem")};
  margin: 0;
`;

export default StyledTreeItem;
